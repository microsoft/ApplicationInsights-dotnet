﻿namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    using System;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;

    internal class SelfDiagnosticsConfigParser
    {
        public const string ConfigFileName = "ApplicationInsightsDiagnostics.json";
        private const int FileSizeLowerLimit = 1024;  // Lower limit for log file size in KB: 1MB
        private const int FileSizeUpperLimit = 128 * 1024;  // Upper limit for log file size in KB: 128MB

        private const string LogDiagnosticsEnvironmentVariable = "APPLICATIONINSIGHTS_LOG_DIAGNOSTICS";

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
            try
            {
                var configFilePath = ConfigFileName;

                // First, check whether the enviornment variable was set.
                if (PlatformSingleton.Current.TryGetEnvironmentVariable(LogDiagnosticsEnvironmentVariable, out string logDiagnosticsPath))
                {
                    configFilePath = Path.Combine(logDiagnosticsPath, ConfigFileName);
                    logDirectory = logDiagnosticsPath;
                }

                // Second, check using current working directory.
                else if (!File.Exists(configFilePath))
                {
#if NET452
                    configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
#else
                    configFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
#endif

                    // Third, check using application base directory.
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
                    
                    if (logDirectory == null && !TryParseLogDirectory(configJson, out logDirectory))
                    {
                        return false;
                    }

                    if (!TryParseFileSize(configJson, out fileSizeInKB))
                    {
                        return false;
                    }

                    if (fileSizeInKB < FileSizeLowerLimit)
                    {
                        fileSizeInKB = FileSizeLowerLimit;
                    }

                    if (fileSizeInKB > FileSizeUpperLimit)
                    {
                        fileSizeInKB = FileSizeUpperLimit;
                    }

                    if (!TryParseLogLevel(configJson, out var logLevelString))
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

        internal static bool TryParseLogLevel(string configJson, out string logLevel)
        {
            var logLevelResult = LogLevelRegex.Match(configJson);
            logLevel = logLevelResult.Groups["LogLevel"].Value;
            return logLevelResult.Success && !string.IsNullOrWhiteSpace(logLevel);
        }
    }
}
