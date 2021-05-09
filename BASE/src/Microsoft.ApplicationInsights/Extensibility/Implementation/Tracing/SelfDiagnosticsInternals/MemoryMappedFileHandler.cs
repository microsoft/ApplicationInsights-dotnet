namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnosticsInternals
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;

    /// <summary>
    /// MemoryMappedFileHandler open a MemoryMappedFile of a certain size at a certain file path.
    /// The class provides a stream object with proper write position.
    /// The stream is cached in ThreadLocal to be thread-safe.
    /// </summary>
    internal class MemoryMappedFileHandler : IDisposable
    {
        /// <summary>
        /// t_memoryMappedFileCache is a handle kept in thread-local storage as a cache to indicate whether the cached
        /// t_viewStream is created from the current m_memoryMappedFile.
        /// </summary>
        private readonly ThreadLocal<MemoryMappedFile> memoryMappedFileCache = new ThreadLocal<MemoryMappedFile>(true);
        private readonly ThreadLocal<MemoryMappedViewStream> viewStream = new ThreadLocal<MemoryMappedViewStream>(true);

#pragma warning disable CA2213 // Disposed in CloseLogFile, which is called in Dispose
        private volatile FileStream underlyingFileStreamForMemoryMappedFile;
        private volatile MemoryMappedFile memoryMappedFile;
#pragma warning restore CA2213 // Disposed in CloseLogFile, which is called in Dispose

        private bool disposedValue;

        /// <summary>
        /// Create a file for MemoryMappedFile. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="filePath">The file path the MemoryMappedFile will be created.</param>
        /// <param name="fileSize">The size of the MemoryMappedFile.</param>
        /// <exception cref="System.Exception">Thrown if file creation failed.</exception>
        public void CreateLogFile(string filePath, int fileSize)
        {
            // Because the API [MemoryMappedFile.CreateFromFile][1](the string version) behaves differently on
            // .NET Framework and .NET Core, here I am using the [FileStream version][2] of it.
            // Taking the last four prameter values from [.NET Framework]
            // (https://referencesource.microsoft.com/#system.core/System/IO/MemoryMappedFiles/MemoryMappedFile.cs,148)
            // and [.NET Core]
            // (https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.cs#L152)
            // The parameter for FileAccess is different in type but the same in rules, both are Read and Write.
            // The parameter for FileShare is different in values and in behavior.
            // .NET Framework doesn't allow sharing but .NET Core allows reading by other programs.
            // The last two parameters are the same values for both frameworks.
            // [1]: https://docs.microsoft.com/dotnet/api/system.io.memorymappedfiles.memorymappedfile.createfromfile?view=net-5.0#System_IO_MemoryMappedFiles_MemoryMappedFile_CreateFromFile_System_String_System_IO_FileMode_System_String_System_Int64_
            // [2]: https://docs.microsoft.com/dotnet/api/system.io.memorymappedfiles.memorymappedfile.createfromfile?view=net-5.0#System_IO_MemoryMappedFiles_MemoryMappedFile_CreateFromFile_System_IO_FileStream_System_String_System_Int64_System_IO_MemoryMappedFiles_MemoryMappedFileAccess_System_IO_HandleInheritability_System_Boolean_
            this.underlyingFileStreamForMemoryMappedFile =
                new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 0x1000, FileOptions.None);

            // The parameter values for MemoryMappedFileSecurity, HandleInheritability and leaveOpen are the same
            // values for .NET Framework and .NET Core:
            // https://referencesource.microsoft.com/#system.core/System/IO/MemoryMappedFiles/MemoryMappedFile.cs,172
            // https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.cs#L168-L179
            this.memoryMappedFile = MemoryMappedFile.CreateFromFile(
                this.underlyingFileStreamForMemoryMappedFile,
                null,
                fileSize,
                MemoryMappedFileAccess.ReadWrite,
#if NET452
                // Only .NET Framework 4.5.2 among all .NET Framework versions is lacking a method omitting this
                // default value for MemoryMappedFileSecurity.
                // https://docs.microsoft.com/dotnet/api/system.io.memorymappedfiles.memorymappedfile.createfromfile?view=netframework-4.5.2
                // .NET Core simply doesn't support this parameter.
                null,
#endif
                HandleInheritability.None,
                false);
        }

        /// <summary>
        /// Get a MemoryMappedViewStream for the MemoryMappedFile object for the current thread.
        /// If no MemoryMappedFile is created yet, return null.
        /// </summary>
        /// <returns>A MemoryMappedViewStream for the MemoryMappedFile object.</returns>
        /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the memory-mapped file is unauthorized.</exception>
        /// <exception cref="System.NullReferenceException">Thrown in a race condition when the memory-mapped file is closed after null check.</exception>
        public MemoryMappedViewStream GetStream()
        {
            if (this.memoryMappedFile == null)
            {
                return null;
            }

            var cachedViewStream = this.viewStream.Value;

            // Each thread has its own MemoryMappedViewStream created from the only one MemoryMappedFile.
            // Once worker thread updates the MemoryMappedFile, all the cached ViewStream objects become
            // obsolete.
            // Each thread creates a new MemoryMappedViewStream the next time it tries to retrieve it.
            // Whether the MemoryMappedViewStream is obsolete is determined by comparing the current
            // MemoryMappedFile object with the MemoryMappedFile object cached at the creation time of the
            // MemoryMappedViewStream.
            if (cachedViewStream == null || this.memoryMappedFileCache.Value != this.memoryMappedFile)
            {
                // Race condition: The code might reach here right after the worker thread sets memoryMappedFile
                // to null in CloseLogFile().
                // In this case, let the NullReferenceException be caught and fail silently.
                // By design, all events captured will be dropped during a configuration file refresh if
                // the file changed, regardless whether the file is deleted or updated.
                cachedViewStream = this.memoryMappedFile.CreateViewStream();
                this.viewStream.Value = cachedViewStream;
                this.memoryMappedFileCache.Value = this.memoryMappedFile;
            }

            return cachedViewStream;
        }

        /// <summary>
        /// Close the all the resources related to the file created for MemoryMappedFile.
        /// </summary>
        public void CloseLogFile()
        {
            MemoryMappedFile mmf = Interlocked.CompareExchange(ref this.memoryMappedFile, null, this.memoryMappedFile);
            if (mmf != null)
            {
                // Each thread has its own MemoryMappedViewStream created from the only one MemoryMappedFile.
                // Once worker thread closes the MemoryMappedFile, all the ViewStream objects should be disposed
                // properly.
                foreach (MemoryMappedViewStream stream in this.viewStream.Values)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }

                mmf.Dispose();
            }

            FileStream fs = Interlocked.CompareExchange(
                ref this.underlyingFileStreamForMemoryMappedFile,
                null,
                this.underlyingFileStreamForMemoryMappedFile);
            fs?.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.CloseLogFile();

                    this.viewStream.Dispose();
                    this.memoryMappedFileCache.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}
