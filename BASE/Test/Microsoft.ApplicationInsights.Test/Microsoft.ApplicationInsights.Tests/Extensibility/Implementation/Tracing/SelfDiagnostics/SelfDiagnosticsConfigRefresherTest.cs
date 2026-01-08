namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using Xunit;
    using System.Text.Json;

    public class SelfDiagnosticsConfigRefresherTest
    {
        private static readonly string ConfigFileName = SelfDiagnosticsConfigParser.ConfigFileName;
        private static readonly byte[] MessageOnNewFile = MemoryMappedFileHandler.MessageOnNewFile;
        private static readonly string MessageOnNewFileString = Encoding.UTF8.GetString(MessageOnNewFile);

        private static readonly Regex TimeStringRegex = new Regex(
            @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [Fact]
        public void SelfDiagnosticsConfigRefresher_OmitAsConfigured()
        {
            var configFilePath = CreateConfigFile();
            try
            {   
                using (var configRefresher = new SelfDiagnosticsConfigRefresher())
                {
                    // Emitting event of EventLevel.Warning
                    CoreEventSource.Log.OperationIsNullWarning();

                    var filePath = configRefresher.CurrentFilePath;

                    int bufferSize = 512;
                    byte[] actualBytes = ReadFile(filePath, bufferSize);
                    string logText = Encoding.UTF8.GetString(actualBytes);
                    Assert.StartsWith(MessageOnNewFileString, logText);

                    // The event was omitted
                    Assert.Equal('\0', (char)actualBytes[MessageOnNewFile.Length]);
                }
            }
            finally
            {
                CleanupConfigFile(configFilePath);
            }
        }

        [Fact]
        public void SelfDiagnosticsConfigRefresher_CaptureAsConfigured()
        {
            var configFilePath = CreateConfigFile();
            try
            {
                using (var configRefresher = new SelfDiagnosticsConfigRefresher())
                {
                    // Emitting event of EventLevel.Error
                    CoreEventSource.Log.TypeWasNotFoundConfigurationError("NoClass");

                    var filePath = configRefresher.CurrentFilePath;

                    int bufferSize = 512;
                    byte[] actualBytes = ReadFile(filePath, bufferSize);
                    string logText = Encoding.UTF8.GetString(actualBytes);
                    Assert.StartsWith(MessageOnNewFileString, logText);

                    // The event was captured
                    string logLine = logText.Substring(MessageOnNewFileString.Length);
                    string logMessage = ParseLogMessage(logLine);
                    string expectedMessage = "ApplicationInsights configuration file loading failed. Type '{0}' was not found. Type loading was skipped. Monitoring will continue.";
                    Assert.StartsWith(expectedMessage, logMessage);
                }
            }
            finally
            {
                CleanupConfigFile(configFilePath);
            }
        }

        [Fact]
        public void SelfDiagnosticsConfigRefresher_ReadFromEnvironmentVariable()
        {
            var key = "APPLICATIONINSIGHTS_LOG_DIAGNOSTICS";
            var val = @"C:\\home\\LogFiles\\SelfDiagnostics";
            if (!Directory.Exists(val))
            {
                val = ".";
            }
            Environment.SetEnvironmentVariable(key, val);

            var configFilePath = CreateConfigFile(false, val);
            try
            {
                using (var configRefresher = new SelfDiagnosticsConfigRefresher())
                {
                    // Emitting event of EventLevel.Error
                    CoreEventSource.Log.TypeWasNotFoundConfigurationError("NoClass");
                    var filePath = configRefresher.CurrentFilePath;

                    int bufferSize = 512;
                    byte[] actualBytes = ReadFile(filePath, bufferSize);
                    string logText = Encoding.UTF8.GetString(actualBytes);
                    Assert.StartsWith(MessageOnNewFileString, logText);

                    // The event was captured
                    string logLine = logText.Substring(MessageOnNewFileString.Length);
                    string logMessage = ParseLogMessage(logLine);
                    string expectedMessage = "ApplicationInsights configuration file loading failed. Type '{0}' was not found. Type loading was skipped. Monitoring will continue.";
                    Assert.StartsWith(expectedMessage, logMessage);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, null);
                CleanupConfigFile(configFilePath);
            }
        }

        private static string ParseLogMessage(string logLine)
        {
            int timestampPrefixLength = "2020-08-14T20:33:24.4788109Z:".Length;
            Assert.True(TimeStringRegex.IsMatch(logLine.Substring(0, timestampPrefixLength)));
            return logLine.Substring(timestampPrefixLength);
        }

        private static byte[] ReadFile(string filePath, int byteCount)
        {
            using (var file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] actualBytes = new byte[byteCount];
                file.Read(actualBytes, 0, byteCount);
                return actualBytes;
            }
        }

        private string CreateConfigFile(bool userDefinedLogDirectory = true, string envVarVal = "")
        {
            ConfigFileObj configFileObj = new()
            {
                FileSize = 1024,
                LogLevel = "Error",
                LogDirectory = userDefinedLogDirectory ? "." : envVarVal
            };

            string configJson = JsonSerializer.Serialize(configFileObj);
            string configFilePath = Path.Combine(configFileObj.LogDirectory, ConfigFileName);
            using (FileStream file = File.Open(configFilePath, FileMode.Create, FileAccess.Write))
            {
                byte[] configBytes = Encoding.UTF8.GetBytes(configJson);
                file.Write(configBytes, 0, configBytes.Length);
            }
            return configFilePath;
        }

        private class ConfigFileObj
        {
            public int FileSize { get; set; }
            public string LogLevel { get; set; }
            public string LogDirectory { get; set; }
        };

        private static void CleanupConfigFile(string configFilePath)
        {
            try
            {
                File.Delete(configFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete config file '{configFilePath}': {ex}");
            }
        }
    }
}