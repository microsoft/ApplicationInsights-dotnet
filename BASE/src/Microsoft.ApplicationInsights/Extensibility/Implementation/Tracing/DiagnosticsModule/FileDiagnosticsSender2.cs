namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.DiagnosticsModule
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;

    using Microsoft.ApplicationInsights.Common.Extensions;

    using static System.FormattableString;

    /// <summary>
    /// This sender works with the DiagnosticTelemetryModule. This will subscribe to events and output to a text file log.
    /// </summary>
    internal class FileDiagnosticsSender2 : IDiagnosticsSender, IDisposable
    {
        private readonly string logFileName = FileHelper.GenerateFileName();
        private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private readonly TimeSpan dequeueInterval = TimeSpan.FromSeconds(30.0);

        private Timer dequeueTimer;

        private string logDirectory = Environment.ExpandEnvironmentVariables("%TEMP%");
        private bool isEnabled = false; // TODO: NEED MORE PERFORMANT FILE WRITTER BEFORE ENABLING THIS BY DEFAULT

        public FileDiagnosticsSender2()
        {
            this.SetAndValidateLogsFolder(this.LogDirectory, this.logFileName);
            this.dequeueTimer = new Timer(new TimerCallback(this.Dequeue));
            this.dequeueTimer.Change(this.dequeueInterval, TimeSpan.FromMilliseconds(0));
        }

        public string LogDirectory 
        {
            get => this.logDirectory;
            set
            {
                if (!this.IsSetByEnvironmentVariable && this.SetAndValidateLogsFolder(value, this.logFileName))
                {
                    this.logDirectory = value;
                }
            }
        }

        public bool Enabled 
        { 
            get => this.isEnabled;
            set => this.isEnabled = this.IsSetByEnvironmentVariable ? this.isEnabled : value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this class was configured via Environment Variable.
        /// If this class is set by the environment variable, lock the other properties to prevent TelemetryConfigurationFactory or customer code from overriding.
        /// We are enabling SysAdmins or DevOps to be able to override this behavior via Environment Variable.
        /// </summary>
        internal bool IsSetByEnvironmentVariable { get; set; }

        /// <summary>
        /// Gets or sets the log file path.
        /// </summary>
        private string LogFilePath { get; set; }

        /// <summary>
        /// Write a trace to file.
        /// </summary>
        /// <param name="eventData">TraceEvent to be written to file.</param>
        public void Send(TraceEvent eventData)
        {
            if (this.Enabled)
            {
                this.queue.Enqueue(Invariant($"{DateTime.UtcNow.ToInvariantString("o")}: {eventData.MetaData.Level}: {eventData}"));
            }
        }

        private static void WriteFileHeader(string logFilePath)
        {
            string[] lines =
            {
                // this.SelfDiagnosticsConfig,
                ".NET SDK version: " + SdkVersionUtils.GetSdkVersion(string.Empty),
                string.Empty,
            };

            System.IO.File.WriteAllLines(logFilePath, lines);
        }

        private bool SetAndValidateLogsFolder(string fileDirectory, string fileName)
        {
            bool result = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(fileDirectory) && !string.IsNullOrWhiteSpace(fileName))
                {
                    // Validate
                    string expandedDirectory = Environment.ExpandEnvironmentVariables(fileDirectory);
                    var logsDirectory = new DirectoryInfo(expandedDirectory);
                    FileHelper.TestDirectoryPermissions(logsDirectory);

                    string fullLogFileName = Path.Combine(expandedDirectory, fileName);

                    // Set
                    this.LogFilePath = fullLogFileName;
                    WriteFileHeader(fullLogFileName);

                    result = true;
                }
            }
            catch (Exception)
            {
                // NotSupportedException: The given path's format is not supported
                // UnauthorizedAccessException
                // ArgumentException: // Path does not specify a valid file path or contains invalid DirectoryInfo characters.
                // DirectoryNotFoundException: The specified path is invalid, such as being on an unmapped drive.
                // IOException: The subdirectory cannot be created. -or- A file or directory already has the name specified by path. -or-  The specified path, file name, or both exceed the system-defined maximum length.
                // SecurityException: The caller does not have code access permission to create the directory.

                // TODO: IS IT SAFE TO LOG HERE?
                // CoreEventSource.Log.LogStorageAccessDeniedError(
                //    error: Invariant($"Path: {this.logDirectory} File: {this.logFileName}; Error: {ex.Message}{Environment.NewLine}"),
                //    user: FileHelper.IdentityName);
            }

            return result;
        }

        private void Dequeue(object state)
        {
            // TODO: STOP TIMER. I think it auto stops, but need to test and confirm.

            if (!this.queue.IsEmpty)
            {
                try
                {
                    FileInfo file = new FileInfo(this.LogFilePath);
                    using (Stream stream = file.Open(FileMode.Append))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        //stream.Position = stream.Length; // I think this is unnecessary if using FileMode.Append
                        
                        while (this.queue.TryDequeue(out string message))
                        {
                            writer.WriteLine(message);
                        }
                    }
                }
                catch (Exception)
                {
                    // no op
                }
            }

            // TODO: RESET TIMER
        }

        public void Dispose()
        {
            this.dequeueTimer.Dispose();
        }
    }
}
