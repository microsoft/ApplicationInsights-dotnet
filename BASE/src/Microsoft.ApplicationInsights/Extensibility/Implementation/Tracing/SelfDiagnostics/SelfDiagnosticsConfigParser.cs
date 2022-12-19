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

        public bool TryGetConfiguration(out string logDirectory, out int fileSizeInKB, out EventLevel logLevel)
        {
            logDirectory = null;
            fileSizeInKB = 0;
            logLevel = EventLevel.LogAlways;

            if (TryGetConfigFromEnvrionmentVariable(ref logDirectory, ref fileSizeInKB, ref logLevel))
            {
                return true;
            }

            return TryGetConfigFromJsonFile(ref logDirectory, ref fileSizeInKB, ref logLevel);
        }

        internal static bool TryGetConfigFromEnvrionmentVariable(ref string logDirectory, ref int fileSizeInKB, ref EventLevel logLevel)
        {
            if (!TryParseLogDirectory(ParseLocation.EnviornmentVariable, Environment.GetEnvironmentVariable(LogDirectory), out logDirectory))
            {
                return false;
            }

            if (!TryParseFileSize(ParseLocation.EnviornmentVariable, Environment.GetEnvironmentVariable(FileSize), out fileSizeInKB))
            {
                return false;
            }

            UpdateFileSizeToBeWithinLimit(ref fileSizeInKB);

            if (!TryParseLogLevel(ParseLocation.EnviornmentVariable, Environment.GetEnvironmentVariable(LogLevel), out var logLevelString))
            {
                return false;
            }

            logLevel = (EventLevel)Enum.Parse(typeof(EventLevel), logLevelString);
            return true;
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

                // user provided configuration json file exists
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

                    if (!TryParseLogLevel(ParseLocation.ConfigJson, configJson, out var logLevelString))
                    {
                        return false;
                    }

                    logLevel = (EventLevel)Enum.Parse(typeof(EventLevel), logLevelString);
                    return true;
                }
            }
            catch (Exception)
            {
                // do nothing on failure to open/read/parse config file
            }

            return false;
        }

        internal static bool TryParseLogDirectory(ParseLocation location, string val, out string logDirectory)
        {
            if (location == ParseLocation.EnviornmentVariable)
            {
                logDirectory = val;
                return !string.IsNullOrWhiteSpace(logDirectory);
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
            fileSizeInKB = 0;
            if (location == ParseLocation.EnviornmentVariable)
            {
                return int.TryParse(val, out fileSizeInKB);
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

        internal static bool TryParseLogLevel(ParseLocation location, string config, out string logLevel)
        {
            if (location == ParseLocation.EnviornmentVariable)
            {
                logLevel = config;
                return !string.IsNullOrEmpty(logLevel);
            }
            else
            {
                var logLevelResult = LogLevelRegex.Match(config);
                logLevel = logLevelResult.Groups[LogLevel].Value;
                return logLevelResult.Success && !string.IsNullOrWhiteSpace(logLevel);
            }
        }

        internal enum ParseLocation 
        { 
            EnviornmentVariable,
            ConfigJson,
        }
    }
}
