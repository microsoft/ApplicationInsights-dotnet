namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    using System;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class SelfDiagnosticsConfigParser
    {
        public const string ConfigFileName = "ApplicationInsightsDiagnostics.json";

        private const int FileSizeLowerLimit = 1024;  // Lower limit for log file size in KB: 1MB
        private const int FileSizeUpperLimit = 128 * 1024;  // Upper limit for log file size in KB: 128MB
        
        private const string LogDirectory = "LogDirectory";
        private const string FileSize = "FileSize";
        private const string LogLevel = "LogLevel";

        /// <summary>
        /// ConfigBufferSize is the maximum bytes of config file that will be read.
        /// </summary>
        private const int ConfigBufferSize = 4 * 1024;

        private static readonly Regex LogDirectoryRegex = new Regex(
            @"""LogDirectory""\s*:\s*""(?<LogDirectory>.*?)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex FileSizeRegex = new Regex(
            @"""FileSize""\s*:\s*(?<FileSize>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LogLevelRegex = new Regex(
            @"""LogLevel""\s*:\s*""(?<LogLevel>.*?)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // This class is called in SelfDiagnosticsConfigRefresher.UpdateMemoryMappedFileFromConfiguration
        // in both main thread and the worker thread.
        // In theory the variable won't be access at the same time because worker thread first Task.Delay for a few seconds.
        private byte[] configBuffer;

        /// <summary>
        /// Represents the location for App Insights to parse user-defined self-diagnostics settings.
        /// </summary>
        internal enum ParseLocation
        {
            /// <summary>
            /// Parse self-diagnostics settings from enviornment variable(s).
            /// </summary>
            EnviornmentVariable,

            /// <summary>
            /// Parse self-diagnostics settings from tje JSON file.
            /// </summary>
            ConfigJson,
        }

        public bool TryGetConfiguration(out string logDirectory, out int fileSizeInKB, out EventLevel logLevel)
        {
            if (TryGetConfigFromEnvrionmentVariable(out logDirectory, out fileSizeInKB, out logLevel))
            {
                return true;
            }

            return this.TryGetConfigFromJsonFile(ref logDirectory, ref fileSizeInKB, ref logLevel);
        }

        internal static bool TryGetConfigFromEnvrionmentVariable(out string logDirectory, out int fileSizeInKB, out EventLevel logLevel)
        {
            logDirectory = null;
            fileSizeInKB = 0;
            logLevel = EventLevel.LogAlways;

            if (!TryParseLogDirectory(ParseLocation.EnviornmentVariable, Environment.GetEnvironmentVariable(LogDirectory), out logDirectory))
            {
                return false;
            }

            if (!TryParseFileSize(ParseLocation.EnviornmentVariable, Environment.GetEnvironmentVariable(FileSize), out fileSizeInKB))
            {
                return false;
            }

            UpdateFileSizeToBeWithinLimit(ref fileSizeInKB);

            if (!TryParseLogLevel(ParseLocation.EnviornmentVariable, Environment.GetEnvironmentVariable(LogLevel), out logLevel))
            {
                return false;
            }

            return true;
        }

        internal static bool TryParseLogDirectory(ParseLocation location, string val, out string logDirectory)
        {
            logDirectory = null;
            if (location == ParseLocation.EnviornmentVariable)
            {
                if (!String.IsNullOrWhiteSpace(val))
                {
                    logDirectory = val;
                    return true;
                }

                // Short circuit for this parse from enviornment variables path
                // to check whether self diagnostics feature was enabled by the json config file.
                return false;
            }
            else 
            {
                var logDirectoryResult = LogDirectoryRegex.Match(val);
                logDirectory = logDirectoryResult.Groups[LogDirectory].Value;
                return logDirectoryResult.Success && !string.IsNullOrWhiteSpace(logDirectory);
            }
        }

        internal static bool TryParseFileSize(ParseLocation location, string val, out int fileSizeInKB)
        {
            fileSizeInKB = FileSizeLowerLimit;
            if (location == ParseLocation.EnviornmentVariable)
            {
                if (!String.IsNullOrEmpty(val))
                {
                    return int.TryParse(val, out fileSizeInKB);
                }

                // If the LogDirectory was set by the environment variable,
                // but FileSize was not set, use the default fileSize.
                return true;
            }
            else
            {
                var fileSizeResult = FileSizeRegex.Match(val);
                return fileSizeResult.Success && int.TryParse(fileSizeResult.Groups[FileSize].Value, out fileSizeInKB);
            }
        }

        // no-op if the fileSize is within range
        internal static void UpdateFileSizeToBeWithinLimit(ref int fileSizeInKB) 
        {
            if (fileSizeInKB < FileSizeLowerLimit)
            {
                fileSizeInKB = FileSizeLowerLimit;
                return;
            }
            
            if (fileSizeInKB > FileSizeUpperLimit)
            {
                fileSizeInKB = FileSizeUpperLimit;
            }
        }

        internal static bool TryParseLogLevel(ParseLocation location, string val, out EventLevel logLevel)
        {
            logLevel = EventLevel.LogAlways;
            if (location == ParseLocation.EnviornmentVariable)
            {
                if (!String.IsNullOrEmpty(val))
                {
                    logLevel = (EventLevel)Enum.Parse(typeof(EventLevel), val);
                }

                // If the LogDirectory was set by the environment variable,
                // but logLevel was not set, use the default logLevel.
                return true;
            }
            else
            {
                var logLevelResult = LogLevelRegex.Match(val);
                var logLevelString = logLevelResult.Groups[LogLevel].Value;
                if (!String.IsNullOrEmpty(logLevelString))
                {
                    logLevel = (EventLevel)Enum.Parse(typeof(EventLevel), logLevelString);
                    return logLevelResult.Success;
                }

                return false;
            }
        }

        internal bool TryGetConfigFromJsonFile(ref string logDirectory, ref int fileSizeInKB, ref EventLevel logLevel)
        {
            try
            {
                var configFilePath = ConfigFileName;

                // First check using current working directory
                if (!File.Exists(configFilePath))
                {
#if NET452
                    configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
#else
                    configFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
#endif

                    // Second check using application base directory
                    if (!File.Exists(configFilePath))
                    {
                        return false;
                    }
                }

                using (FileStream file = File.Open(configFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var buffer = this.configBuffer;
                    if (buffer == null)
                    {
                        buffer = new byte[ConfigBufferSize]; // Fail silently if OOM
                        this.configBuffer = buffer;
                    }

                    file.Read(buffer, 0, buffer.Length);
                    string configJson = Encoding.UTF8.GetString(buffer);
                    if (!TryParseLogDirectory(ParseLocation.ConfigJson, configJson, out logDirectory))
                    {
                        return false;
                    }

                    if (!TryParseFileSize(ParseLocation.ConfigJson, configJson, out fileSizeInKB))
                    {
                        return false;
                    }

                    UpdateFileSizeToBeWithinLimit(ref fileSizeInKB);

                    if (!TryParseLogLevel(ParseLocation.ConfigJson, configJson, out logLevel))
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception)
            {
                // do nothing on failure to open/read/parse config file
            }

            return false;
        }
    }
}
