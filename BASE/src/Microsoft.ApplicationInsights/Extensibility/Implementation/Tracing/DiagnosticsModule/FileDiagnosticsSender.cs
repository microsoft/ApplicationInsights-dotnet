namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsModule
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.ApplicationInsights.Common.Extensions;

    using static System.FormattableString;

    /// <summary>
    /// This sender works with the DiagnosticTelemetryModule. This will subscribe to events and output to a text file log.
    /// </summary>
    internal class FileDiagnosticsSender : IDiagnosticsSender, IDisposable
    {
        private readonly DefaultTraceListener defaultTraceListener;
        private bool disposedValue;
        private string logFileName = FileHelper.GenerateFileName();
        private string logDirectory = "C:\\TEMP\\"; // Environment.ExpandEnvironmentVariables("%TEMP%");
        private object lockObj = new object();

        public FileDiagnosticsSender()
        {
            this.defaultTraceListener = new DefaultTraceListener();
            this.SetAndValidateLogsFolder(this.LogDirectory, this.logFileName);

        }

        public string LogDirectory 
        {
            get => this.logDirectory;
            set
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(value);
                if (this.SetAndValidateLogsFolder(expandedPath, this.logFileName))
                {
                    this.logDirectory = expandedPath;
                }
            }
        }

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets the log file path.
        /// </summary>
        public string LogFilePath { get; set; }
        //{
        //    get => this.defaultTraceListener.LogFileName;
        //    private set => this.defaultTraceListener.LogFileName = value;
        //}

        public string Severity { get; set; }

        /// <summary>
        /// Write a trace to file.
        /// </summary>
        /// <param name="eventData">TraceEvent to be written to file.</param>
        public void Send(TraceEvent eventData)
        {
            if (this.Enabled)
            {
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceSource.cs,239
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceEventCache.cs,46
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceListener.cs,409
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/DefaultTraceListener.cs,131

                var message = Invariant($"{DateTime.UtcNow.ToInvariantString("o")}: {eventData.MetaData.Level}: {eventData}");
                //this.defaultTraceListener.WriteLine(message);

                lock (this.lockObj)
                {
                    try
                    {
                        FileInfo file = new FileInfo(this.LogFilePath);
                        using (Stream stream = file.Open(FileMode.OpenOrCreate))
                        {
                            using (StreamWriter writer = new StreamWriter(stream))
                            {
                                stream.Position = stream.Length;
                                writer.WriteLine(message);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // no op
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.defaultTraceListener.Dispose();
                }

                this.disposedValue = true;
            }
        }

        private bool SetAndValidateLogsFolder(string filePath, string fileName)
        {
            bool result = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(filePath) && !string.IsNullOrWhiteSpace(fileName))
                {
                    // Validate
                    var logsDirectory = new DirectoryInfo(filePath);
                    FileHelper.TestDirectoryPermissions(logsDirectory);

                    string fullLogFileName = Path.Combine(filePath, fileName);
                    CoreEventSource.Log.LogsFileName(fullLogFileName);

                    // Set
                    this.LogFilePath = fullLogFileName;

                    result = true;
                }
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
                    error: Invariant($"Path: {this.logDirectory} File: {this.logFileName}; Error: {ex.Message}{Environment.NewLine}"),
                    user: FileHelper.IdentityName);
            }

            return result;
        }

        private void WriteFileHeader(string logFilePath)
        {
            string[] lines =
            {
                // this.SelfDiagnosticsConfig,
                ".NET SDK version: " + SdkVersionUtils.GetSdkVersion(string.Empty),
                "Severity: " + this.Severity,
                string.Empty,
            };

            System.IO.File.WriteAllLines(logFilePath, lines);
        }
    }
}
