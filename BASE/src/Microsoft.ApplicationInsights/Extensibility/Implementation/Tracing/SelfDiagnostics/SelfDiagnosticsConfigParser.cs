namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class SelfDiagnosticsConfigParser
    {
        public const string ConfigFileName = "ApplicationInsightsDiagnostics.json";

        private const int FileSizeLowerLimit = 1024;  // Lower limit for log file size in KB: 1MB
        private const int FileSizeUpperLimit = 128 * 1024;  // Upper limit for log file size in KB: 128MB

        private const string APPLICATIONINSIGHTS_LOG_DIAGNOSTICS = "APPLICATIONINSIGHTS_LOG_DIAGNOSTICS";
        private const string APPLICATIONINSIGHTS_LOG_LOGDIRECTORY = "LogDirectory";
        private const string APPLICATIONINSIGHTS_LOG_FILESIZE = "FileSize";
        private const string APPLICATIONINSIGHTS_LOG_LOGLEVEL = "LogLevel";

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
            if (TryGetConfigFromEnvrionmentVariable(out logDirectory, out fileSizeInKB, out logLevel))
            {
                // Self-diagnostics config passed in via enviornment variables has higher precedence than
                // self-diagnostcis config passed in via JSON file.
                return true;
            }

            return this.TryGetConfigFromJsonFile(ref logDirectory, ref fileSizeInKB, ref logLevel);
        }

        internal static bool TryGetConfigFromEnvrionmentVariable(out string logDirectory, out int fileSizeInKB, out EventLevel logLevel)
        {
            logDirectory = null;
            fileSizeInKB = FileSizeLowerLimit;
            logLevel = EventLevel.Error;

            if (!PlatformSingleton.Current.TryGetEnvironmentVariable(APPLICATIONINSIGHTS_LOG_DIAGNOSTICS, out string ApplicationInsightsDiagnosticsVal))
            {
                // enviornment varaiable was not set successfully
                return false;
            }

            // remove all whitespaces
            ApplicationInsightsDiagnosticsVal = Regex.Replace(ApplicationInsightsDiagnosticsVal, @"\s+", "");

            var keyValuePairs = ApplicationInsightsDiagnosticsVal.Split(',')
                .Select(value => value.Split('='))
                .ToDictionary(pair => pair[0], pair => pair[1]);
            var concurrentDictionary = new ConcurrentDictionary<string, string>(keyValuePairs);

            if (!concurrentDictionary.TryGetValue(APPLICATIONINSIGHTS_LOG_LOGDIRECTORY, out logDirectory) || string.IsNullOrWhiteSpace(logDirectory))
            {
                return false;
            }

            // if user passed in optional parameter but it is invalid, return early
            if (concurrentDictionary.TryGetValue(APPLICATIONINSIGHTS_LOG_FILESIZE, out var fileSizeString) && !int.TryParse(fileSizeString, out fileSizeInKB)) 
            {
                return false;
            }

            UpdateFileSizeToBeWithinLimit(ref fileSizeInKB);

            // if user passed in optional parameter but it is invalid, return early
            if (concurrentDictionary.TryGetValue(APPLICATIONINSIGHTS_LOG_LOGLEVEL, out var logLevelString) && !Enum.TryParse(logLevelString, false /*case-insensitive*/, out logLevel))
            {
                return false;
            }

            return true;
        } 

        internal static bool TryParseLogDirectory(string configJson, out string logDirectory)
        {
            var logDirectoryResult = LogDirectoryRegex.Match(configJson);
            logDirectory = logDirectoryResult.Groups["LogDirectory"].Value;
            return logDirectoryResult.Success && !string.IsNullOrWhiteSpace(logDirectory);
        }

        internal static bool TryParseFileSize(string configJson, out int fileSizeInKB)
        {
            fileSizeInKB = 0;
            var fileSizeResult = FileSizeRegex.Match(configJson);
            return fileSizeResult.Success && int.TryParse(fileSizeResult.Groups["FileSize"].Value, out fileSizeInKB);
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

        internal static bool TryParseLogLevel(string configJson, out EventLevel logLevel)
        {
            logLevel = EventLevel.Error;
            var logLevelResult = LogLevelRegex.Match(configJson);
            if (!logLevelResult.Success) 
            {
                return false;
            }

            var logLevelString = logLevelResult.Groups[APPLICATIONINSIGHTS_LOG_LOGLEVEL].Value;
            if (string.IsNullOrWhiteSpace(logLevelString))
            {
                return false;
            }
            
            if (!Enum.TryParse(logLevelString, false, out logLevel))
            {
                return false;
            }

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
                    if (!TryParseLogDirectory(configJson, out logDirectory))
                    {
                        return false;
                    }

                    if (!TryParseFileSize(configJson, out fileSizeInKB))
                    {
                        return false;
                    }

                    UpdateFileSizeToBeWithinLimit(ref fileSizeInKB);

                    if (!TryParseLogLevel(configJson, out logLevel))
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
