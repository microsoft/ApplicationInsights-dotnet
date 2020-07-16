namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsModule
{
    using System;
    using System.IO;

    using static System.FormattableString;

    /// <summary>
    /// This sender works with the DiagnosticTelemetryModule. This will subscribe to events and output to a text file log.
    /// </summary>
    internal class FileDiagnosticsSender : IDiagnosticsSender
    {
        public FileDiagnosticsSender()
        {
            this.CreateNewFile();
        }

        /// <summary>
        /// Gets or sets the self-diagnostics configuration string used to setup this module.
        /// </summary>
        /// <remarks>
        /// This module can be setup by the Self-Diagnostics Environment Variable and we want to include that string in the file.
        /// </remarks>
        internal string SelfDiagnosticsConfig { get; set; }

        internal string FileDirectory { get; set; } = Environment.ExpandEnvironmentVariables("%TEMP%");

        /// <summary>
        /// Gets or sets a value indicating the max size of a flog file.
        /// Int32.MaxValue = 2,147,483,648 which is 2.1 Gigabytes.
        /// </summary>
        internal int MaxSizeBytes { get; set; }

        private FileInfo LogFile { get; set; }

        /// <summary>
        /// Write a trace to file.
        /// </summary>
        /// <remarks>
        /// Copied from DefaultTraceListener https://referencesource.microsoft.com/#System/compmod/system/diagnostics/DefaultTraceListener.cs,131 .
        /// </remarks>
        /// <param name="eventData">TraceEvent to be written to file.</param>
        public void Send(TraceEvent eventData)
        {
            try
            {
                using (Stream stream = this.LogFile.Open(FileMode.OpenOrCreate))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    stream.Position = stream.Length;
                    writer.WriteLine(eventData.ToString());
                }
            }
            catch (Exception)
            {
                // We were trying to send traces out and failed. 
                // No reason to try to trace something else again
            }
            finally
            {
                if (this.LogFile.Length > this.MaxSizeBytes)
                {
                    this.CreateNewFile();
                }
            }
        }

        private void CreateNewFile()
        {
            try
            {
                var directory = new DirectoryInfo(this.FileDirectory);
                FileHelper.TestDirectoryPermissions(directory);

                var fileName = FileHelper.GenerateFileName();
                string filePath = Path.Combine(directory.FullName, fileName);
                this.LogFile = new FileInfo(filePath);

                string[] fileHeader =
                {
                    this.SelfDiagnosticsConfig,
                    ".NET SDK version: " + SdkVersionUtils.GetSdkVersion(string.Empty),
                    string.Empty,
                };

                System.IO.File.WriteAllLines(filePath, fileHeader);
            }
            catch (Exception ex)
            {
                // NotSupportedException: The given path's format is not supported
                // UnauthorizedAccessException
                // ArgumentException: // Path does not specify a valid file path or contains invalid DirectoryInfo characters.
                // DirectoryNotFoundException: The specified path is invalid, such as being on an unmapped drive.
                // IOException: The subdirectory cannot be created. -or- A file or directory already has the name specified by path. -or-  The specified path, file name, or both exceed the system-defined maximum length.
                // SecurityException: The caller does not have code access permission to create the directory.

                CoreEventSource.Log.LogStorageAccessDeniedError(
                    error: Invariant($"Path: {this.FileDirectory} Error: {ex.Message}"),
                    user: FileHelper.IdentityName);
            }
        }
    }
}
