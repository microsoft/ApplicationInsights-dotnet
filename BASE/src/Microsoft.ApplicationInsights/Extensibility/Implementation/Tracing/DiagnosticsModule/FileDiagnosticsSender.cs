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
    internal class FileDiagnosticsSender : IDiagnosticsSender
    {
        private string logFileName = FileHelper.GenerateFileName();
        private string logDirectory = Environment.ExpandEnvironmentVariables("%TEMP%");
        private object lockObj = new object();
        private bool isEnabled = false; // TODO: NEED MORE PERFORMANT FILE WRITTER BEFORE ENABLING THIS BY DEFAULT

        public FileDiagnosticsSender()
        {
            this.SetAndValidateLogsFolder(this.LogDirectory, this.logFileName);
        }

        public string LogDirectory 
        {
            get => this.logDirectory;
            set
            {
                if (this.isEnabled)
                {
                    return;
                }

                string expandedPath = Environment.ExpandEnvironmentVariables(value);
                if (this.SetAndValidateLogsFolder(expandedPath, this.logFileName))
                {
                    this.logDirectory = expandedPath;
                }
            }
        }

        public bool Enabled 
        { 
            get => this.isEnabled;
            set => this.isEnabled = this.IsSetByEnvironmentVariable ? this.isEnabled : value;
        }

        /// <summary>
        /// Gets or sets the log file path.
        /// </summary>
        public string LogFilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this class was configured via Environment Variable.
        /// If this class is set by the environment variable, lock the other properties to prevent TelemetryConfigurationFactory or customer code from overriding.
        /// We are enabling SysAdmins or DevOps to be able to override this behavior via Environment Variable.
        /// </summary>
        internal bool IsSetByEnvironmentVariable { get; set; }

        /// <summary>
        /// Write a trace to file.
        /// </summary>
        /// <param name="eventData">TraceEvent to be written to file.</param>
        public void Send(TraceEvent eventData)
        {
            if (this.Enabled)
            {
                // We previously depended on the DefaultTraceListener for writing to file. 
                // This has some overhead, but the path we were utilizing calls a lock and uses a StreamWriter.
                // I've copied the implementation below, but this should be replaced to be more performant.
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceSource.cs,239
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceEventCache.cs,46
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceListener.cs,409
                // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/DefaultTraceListener.cs,131

                var message = Invariant($"{DateTime.UtcNow.ToInvariantString("o")}: {eventData.MetaData.Level}: {eventData}");

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
    }
}
