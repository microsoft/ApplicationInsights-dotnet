namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
#if !NETSTANDARD1_3

    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Threading;

    using Microsoft.ApplicationInsights.Common.Extensions;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.FileDiagnosticsModule;

    using static System.FormattableString;

    /// <summary>
    /// Diagnostics telemetry module for azure web sites.
    /// </summary>
    public class FileDiagnosticsTelemetryModule : IDisposable, ITelemetryModule
    {
        private readonly TraceSourceForEventSource traceSource = new TraceSourceForEventSource(EventLevel.Error);
        private readonly DefaultTraceListener listener = new DefaultTraceListener();

        private string windowsIdentityName;
        private string logFileName;
        private string logFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDiagnosticsTelemetryModule" /> class.
        /// </summary>
        public FileDiagnosticsTelemetryModule()
        {
            this.logFilePath = Environment.ExpandEnvironmentVariables("%TEMP%");

            var process = Process.GetCurrentProcess();
            this.logFileName = Invariant($"ApplicationInsightsLog_{DateTime.UtcNow.ToInvariantString("yyyyMMdd_HHmmss")}_{process.ProcessName}_{process.Id}.txt");

            this.SetAndValidateLogsFolder(this.logFilePath, this.logFileName);

            this.listener.TraceOutputOptions |= TraceOptions.DateTime | TraceOptions.ProcessId;
            this.traceSource.Listeners.Add(this.listener);
        }

        /// <summary>
        /// Gets or sets diagnostics Telemetry Module LogLevel configuration setting.
        /// </summary>
        public string Severity
        {
            get => this.traceSource.LogLevel.ToString();

            set
            {
                // Once logLevel is set from configuration, restart listener with new value
                if (!string.IsNullOrEmpty(value))
                {
                    EventLevel parsedValue;
                    if (Enum.IsDefined(typeof(EventLevel), value) == true)
                    {
                        parsedValue = (EventLevel)Enum.Parse(typeof(EventLevel), value, true);
                        this.traceSource.LogLevel = parsedValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets log file name.
        /// </summary>
        public string LogFileName
        {
            get => this.logFileName;

            set
            {
                if (this.SetAndValidateLogsFolder(this.logFilePath, value))
                {
                    this.logFileName = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets log file path.
        /// </summary>
        public string LogFilePath
        {
            get => this.logFilePath;

            set
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(value);                
                if (this.SetAndValidateLogsFolder(expandedPath, this.logFileName))
                {
                    this.logFilePath = expandedPath;
                }
            }
        }

        /// <summary>
        /// Gets or sets the self-diagnostics configuration string used to setup this module.
        /// </summary>
        /// <remarks>
        /// This module can be setup by the Self-Diagnostics Environment Variable and we want to include that string in the file.
        /// </remarks>
        internal string SelfDiagnosticsConfig { get; set; }

        /// <summary>
        /// No op.
        /// </summary>
        /// <param name="configuration">Telemetry configuration object.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
        }        

        /// <summary>
        /// Disposes event listener.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes event listener.
        /// </summary>
        protected virtual void Dispose(bool disposeManaged = true)
        {
            if (disposeManaged)
            {
                this.listener.Dispose();
                this.traceSource.Dispose();
            }
        }

        /// <summary>
        /// Create a random named file and write to it to confirm that we have access to this directory. 
        /// Deletes flie when FileStream is closed.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Throws <see cref="UnauthorizedAccessException" /> if the process lacks the required permissions to access the <paramref name="directory"/>.</exception>
        private static void CheckAccessPermissions(DirectoryInfo directory)
        {
            string testFileName = Path.GetRandomFileName();
            string testFilePath = Path.Combine(directory.FullName, testFileName);

            if (!Directory.Exists(directory.FullName))
            {
                Directory.CreateDirectory(directory.FullName);
            }

            // Create a test file 
            using (var testFile = new FileStream(testFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                testFile.Write(new[] { default(byte) }, 0, 1);
            }
        }

        private void WriteFileHeader(string logFilePath)
        {
            string[] lines = 
            { 
                this.SelfDiagnosticsConfig,
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
                    var logsDirectory = new DirectoryInfo(filePath);

                    CheckAccessPermissions(logsDirectory);

                    string fullLogFileName = Path.Combine(filePath, fileName);
                    CoreEventSource.Log.LogsFileName(fullLogFileName);

                    this.WriteFileHeader(fullLogFileName);

                    this.listener.LogFileName = fullLogFileName;

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
                    error: Invariant($"Path: {this.logFilePath} File: {this.logFileName}; Error: {ex.Message}{Environment.NewLine}"),
                    user: LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }

            return result;
        }

        private string GetCurrentIdentityName()
        {
            string result = string.Empty;

            try
            {
#if NETSTANDARD2_0
                result = System.Environment.UserName;
#else
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    result = identity.Name;
                }
#endif
            }
            catch (SecurityException exp)
            {
                CoreEventSource.Log.LogWindowsIdentityAccessSecurityException(exp.Message);
            }

            return result;
        }
    }
#endif
}
