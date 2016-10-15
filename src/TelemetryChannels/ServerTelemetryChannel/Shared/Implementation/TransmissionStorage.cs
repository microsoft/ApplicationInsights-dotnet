namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class TransmissionStorage
    {
        internal const string TemporaryFileExtension = ".tmp";
        internal const string TransmissionFileExtension = ".trn";
        internal const int DefaultCapacityKiloBytes = 50 * 1024;

        private readonly ConcurrentQueue<IPlatformFile> files;
        private readonly object loadFilesLock;
        
        private IPlatformFolder folder;
        private long capacity = DefaultCapacityKiloBytes * 1024;
        private long size;
        private bool sizeCalculated;
        private Random random = new Random();

        public TransmissionStorage()
        {
            this.files = new ConcurrentQueue<IPlatformFile>();
            this.loadFilesLock = new object();
            this.sizeCalculated = false;
        }

        /// <summary>
        /// Gets or sets the total amount of disk space, in bytes, allowed for storing transmission files.
        /// </summary>
        public virtual long Capacity
        {
            get
            {
                return this.capacity;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this.capacity = value;
            }
        }

        public virtual void Initialize(IApplicationFolderProvider applicationFolderProvider)
        {
            if (applicationFolderProvider == null)
            {
                throw new ArgumentNullException("applicationFolderProvider");
            }

            this.folder = applicationFolderProvider.GetApplicationFolder();
        }

        public virtual bool Enqueue(Func<Transmission> transmissionGetter)
        {
            if (this.folder == null)
            {
                return false;
            }

            this.EnsureSizeIsCalculated();
            
            if (this.size < this.Capacity)
            {
                var transmission = transmissionGetter();
                if (transmission == null)
                {
                    return false;
                }

                try
                {
                    IPlatformFile temporaryFile = this.CreateTemporaryFile();
                    long temporaryFileSize = SaveTransmissionToFile(transmission, temporaryFile);
                    ChangeFileExtension(temporaryFile, TransmissionFileExtension);
                    Interlocked.Add(ref this.size, temporaryFileSize);
                    TelemetryChannelEventSource.Log.TransmissionSavedToStorage(transmission.Id);
                    return true;
                }
                catch (UnauthorizedAccessException e)
                {
                    // Expected because the process may have lost permission to access the folder or the files in it.
                    TelemetryChannelEventSource.Log.UnauthorizedAccessExceptionOnTransmissionSaveWarning(transmission.Id, e.Message);
                }
                catch (Exception exp)
                {
                    TelemetryChannelEventSource.Log.TransmissionFailedToStoreWarning(transmission.Id, exp.ToString());
                    throw;
                }
            }
            else
            {
                TelemetryChannelEventSource.Log.StorageEnqueueNoCapacityWarning(this.size, this.Capacity);
            }

            return false;
        }

        public virtual Transmission Dequeue()
        {
            if (this.folder == null)
            {
                return null;
            }

            this.EnsureSizeIsCalculated();
            
            string lastInaccessibleFileName = null;

            while (true)
            {
                IPlatformFile file = null;
                try
                {
                    file = this.GetOldestTransmissionFileOrNull();
                    if (file == null)
                    {
                        return null; // Because there are no more transmission files.
                    }

                    long fileSize;
                    Transmission transmission = LoadFromTransmissionFile(file, out fileSize);
                    if (transmission != null)
                    {
                        Interlocked.Add(ref this.size, -fileSize);
                        return transmission;
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    TelemetryChannelEventSource.Log.TransmissionStorageDequeueUnauthorizedAccessException(file?.Name ?? string.Empty, uae.ToString());
                    if (file == null)
                    {
                        return null; // Because the process does not have permission to access the folder.
                    }

                    if (lastInaccessibleFileName != file.Name)
                    {
                        lastInaccessibleFileName = file.Name;
                        Thread.Sleep(random.Next(1, 100)); // Sleep for random time of 1 to 100 milliseconds to try to avoid future timing conflicts.
                        continue; // Because another thread is loading this file right now.
                    }
                    else
                    {
                        // The same file has been inaccessible more than once.
                        TelemetryChannelEventSource.Log.TransmissionStorageInaccessibleFile(file.Name);
                    }

                    throw; // Because the process does not have permission to modify this file.
                }
                catch (IOException ioe)
                {
                    TelemetryChannelEventSource.Log.TransmissionStorageDequeueIOError(file.Name, ioe.ToString());
                    Thread.Sleep(random.Next(1, 100)); // Sleep for random time of 1 to 100 milliseconds to try to avoid future timing conflicts.
                    continue; // It may be because another thread already loaded this file, we don't know yet.
                }
            }
        }

        private static string GetUniqueFileName(string extension)
        {
            string fileName = Guid.NewGuid().ToString("N");
            return Path.ChangeExtension(fileName, extension);
        }

        private static Transmission LoadFromTransmissionFile(IPlatformFile file, out long fileSize)
        {
            fileSize = 0;
            Transmission transmission = null;
            if (file.Exists)
            {
                if (file.DateCreated > DateTimeOffset.Now.AddDays(-2))
                {
                    ChangeFileExtension(file, TemporaryFileExtension);
                    transmission = LoadFromTemporaryFile(file, out fileSize);
                }
                else
                {
                    TelemetryChannelEventSource.Log.TransmissionStorageFileExpired(file.Name, file.DateCreated.ToString());
                }

                file.Delete();
            }

            return transmission;
        }

        private static Transmission LoadFromTemporaryFile(IPlatformFile file, out long fileSize)
        {
            using (Stream stream = file.Open())
            {
                try
                {
                    fileSize = stream.Length;
                    return TransmissionExtensions.Load(stream);
                }
                catch (FormatException exp)
                {
                    fileSize = 0;
                    TelemetryChannelEventSource.Log.IncorrectFileFormatWarning(exp.Message);
                    return null;
                }
            }
        }

        private static void ChangeFileExtension(IPlatformFile file, string extension)
        {
            string transmissionFileName = Path.ChangeExtension(file.Name, extension);
            file.Rename(transmissionFileName);
        }

        private static long SaveTransmissionToFile(Transmission transmission, IPlatformFile file)
        {
            using (Stream stream = file.Open())
            {
                transmission.Save(stream);
                return stream.Length;
            }
        }

        private IPlatformFile CreateTemporaryFile()
        {
            string temporaryFileName = GetUniqueFileName(TemporaryFileExtension);
            return this.folder.CreateFile(temporaryFileName);
        }

        private IEnumerable<IPlatformFile> GetTransmissionFiles()
        {
            IEnumerable<IPlatformFile> newFiles = this.folder.GetFiles();
            newFiles = newFiles.Where(f => f.Extension == TransmissionFileExtension);
            return newFiles;
        }

        private IPlatformFile GetOldestTransmissionFileOrNull()
        {
            IPlatformFile file;
            if (!this.files.TryDequeue(out file))
            {
                this.LoadFilesOrderedByDateFromFolder();
                this.files.TryDequeue(out file);
            }

            return file;
        }

        private void LoadFilesOrderedByDateFromFolder()
        {
            if (this.files.Count == 0)
            {
                lock (this.loadFilesLock)
                {
                    if (this.files.Count == 0)
                    {
                        IEnumerable<IPlatformFile> newFiles = this.GetTransmissionFiles();
                        foreach (IPlatformFile file in newFiles.OrderBy(f => f.DateCreated))
                        {
                            this.files.Enqueue(file);
                        }
                    }
                }
            }
        }

        private void EnsureSizeIsCalculated()
        {
            if (!this.sizeCalculated)
            {
                lock (this.loadFilesLock)
                {
                    if (!this.sizeCalculated)
                    {
                        try
                        {
                            var storageFiles = this.GetTransmissionFiles();

                            long newSize = 0;
                            foreach (IPlatformFile platformFile in storageFiles)
                            {
                                try
                                {
                                    newSize += platformFile.Length;
                                }
                                catch (FileNotFoundException)
                                {
                                    continue; // Because another thread already dequeued this transmission file.
                                }
                            }

                            Interlocked.Exchange(ref this.size, newSize);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            TelemetryChannelEventSource.Log.UnauthorizedAccessExceptionOnCalculateSizeWarning(e.Message);
                            this.size = long.MaxValue;
                        }

                        this.sizeCalculated = true;
                        TelemetryChannelEventSource.Log.StorageSize(this.size);
                    }
                }
            }
        }
    }
}
