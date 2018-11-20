namespace Microsoft.ApplicationInsights.Extensibility.HostingStartup
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.IO;
    using System.Security;
    using System.Security.Principal;
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    
    /// <summary>
    /// Diagnostics telemetry module for azure web sites.
    /// </summary>
    public class FileDiagnosticsTelemetryModule : IDisposable, ITelemetryModule
    {
        private string windowsIdentityName;
        
        private string logFileName;
        private string logFilePath;

        private TraceSourceForEventSource traceSource = new TraceSourceForEventSource(EventLevel.Error);
        private DefaultTraceListener listener = new DefaultTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDiagnosticsTelemetryModule" /> class.
        /// </summary>
        public FileDiagnosticsTelemetryModule()
        {
            this.logFilePath = Environment.ExpandEnvironmentVariables("%TEMP%");
            this.logFileName = "ApplicationInsightsLog_" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + ".txt";

            this.SetAndValidateLogsFolder(this.logFilePath, this.logFileName);

            this.listener.TraceOutputOptions |= TraceOptions.DateTime | TraceOptions.ProcessId;
            this.traceSource.Listeners.Add(this.listener);
        }

        /// <summary>
        /// Gets or sets diagnostics Telemetry Module LogLevel configuration setting.
        /// </summary>
        public string Severity
        {
            get
            {
                return this.traceSource.LogLevel.ToString();
            }

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
            get
            {
                return this.logFileName;
            }

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
            get
            {
                return this.logFilePath;
            }

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
        /// Initializes the telemetry module.
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
        /// Throws <see cref="UnauthorizedAccessException" /> if the process lacks the required permissions to access the <paramref name="directory"/>.
        /// </summary>
        private static void CheckAccessPermissions(DirectoryInfo directory)
        {
            string testFileName = Path.GetRandomFileName();
            string testFilePath = Path.Combine(directory.FullName, testFileName);

            if (!Directory.Exists(directory.FullName))
            {
                Directory.CreateDirectory(directory.FullName);
            }

            // FileSystemRights.CreateFiles
            using (var testFile = new FileStream(testFilePath, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                // FileSystemRights.Write
                testFile.Write(new[] { default(byte) }, 0, 1);
            }
        }

        private static string GetPathAccessFailureErrorMessage(Exception exp, string path, string file)
        {
            return "Path: " + path + "File: " + file + "; Error: " + exp.Message + Environment.NewLine;
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
                    HostingStartupEventSource.Log.LogsFileName(fullLogFileName);

                    this.listener.LogFileName = fullLogFileName;

                    result = true;
                }
            }
            catch (NotSupportedException exp)
            {
                // The given path's format is not supported
                HostingStartupEventSource.Log.LogStorageAccessDeniedError(
                    GetPathAccessFailureErrorMessage(exp, this.logFilePath, this.logFileName),
                    LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }
            catch (UnauthorizedAccessException exp)
            {
                HostingStartupEventSource.Log.LogStorageAccessDeniedError(
                    GetPathAccessFailureErrorMessage(exp, this.logFilePath, this.logFileName),
                    LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }
            catch (ArgumentException exp)
            {
                // Path does not specify a valid file path or contains invalid DirectoryInfo characters.
                HostingStartupEventSource.Log.LogStorageAccessDeniedError(
                    GetPathAccessFailureErrorMessage(exp, this.logFilePath, this.logFileName),
                    LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }
            catch (DirectoryNotFoundException exp)
            {
                // The specified path is invalid, such as being on an unmapped drive.
                HostingStartupEventSource.Log.LogStorageAccessDeniedError(
                   GetPathAccessFailureErrorMessage(exp, this.logFilePath, this.logFileName),
                   LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }
            catch (IOException exp)
            {
                // The subdirectory cannot be created. -or- A file or directory already has the name specified by path. -or-  The specified path, file name, or both exceed the system-defined maximum length. .
                HostingStartupEventSource.Log.LogStorageAccessDeniedError(
                   GetPathAccessFailureErrorMessage(exp, this.logFilePath, this.logFileName),
                   LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }
            catch (SecurityException exp)
            {
                // The caller does not have code access permission to create the directory.
                HostingStartupEventSource.Log.LogStorageAccessDeniedError(
                    GetPathAccessFailureErrorMessage(exp, this.logFilePath, this.logFileName),
                    LazyInitializer.EnsureInitialized(ref this.windowsIdentityName, this.GetCurrentIdentityName));
            }

            return result;
        }

        private string GetCurrentIdentityName()
        {
            string result = string.Empty;

            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    result = identity.Name;
                }
            }
            catch (SecurityException exp)
            {
                HostingStartupEventSource.Log.LogWindowsIdentityAccessSecurityException(exp.Message);
            }

            return result;
        }
    }
}
