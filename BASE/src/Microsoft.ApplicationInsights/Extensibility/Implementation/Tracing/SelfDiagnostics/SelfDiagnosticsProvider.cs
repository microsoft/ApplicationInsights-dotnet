namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.ConfigString;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;

    using static System.FormattableString;

    /// <summary>
    /// This class encapsulates parsing and interpreting the self diagnostics configuration string.
    /// </summary>
    internal static class SelfDiagnosticsProvider
    {
        internal const string SelfDiagnosticsEnvironmentVariable = "APPLICATIONINSIGHTS_SELF_DIAGNOSTICS";
        internal const string KeyDestination = "Destination";
        internal const string KeyDirectory = "Directory";
        internal const string KeyLevel = "Level";
        internal const string ValueFile = "file";
        internal const string DefaultDirectory = "%TEMP%";
        internal const EventLevel DefaultLevel = EventLevel.Verbose;

        /// <summary>
        /// Parse the environment variable and determine if self diagnostics logging has been enabled. 
        /// </summary>
        /// <returns>If the config is valid, will return an instance of the generic class specified. If not, will return null.</returns>
        /// <exception cref="Exception">
        /// Throws an exception if the configuration string is invalid. 
        /// This is expected to crash the application and provide immediate feedback to the end user if the config is invalid.
        /// </exception>
        internal static T EvaluateSelfDiagnosticsConfig<T>() where T : ISelfDiagnosticsFileWriter, new()
        { 
            try
            {
                if (PlatformSingleton.Current.TryGetEnvironmentVariable(SelfDiagnosticsEnvironmentVariable, out string selfDiagnosticsConfigurationString)
                    && IsFileDiagnosticsEnabled(selfDiagnosticsConfigurationString, out string directory, out string level))
                {
                    var instance = new T();
                    instance.Initialize(level: level, fileDirectory: directory);
                    return instance;
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Self-Diagnostics config string. You must fix or remove this configuration.", ex);
            }
        }

        /// <summary>
        /// Parse a configuration string and return a Dictionary.
        /// </summary>
        /// <remarks>Example: "key1=value1;key2=value2;key3=value3".</remarks>
        /// <returns>A dictionary parsed from the input configuration string.</returns>
        internal static IDictionary<string, string> ParseConfigurationString(string configurationString)
        {
            IDictionary<string, string> keyValuePairs;

            try
            {
                keyValuePairs = ConfigStringParser.Parse(configurationString);
            }
            catch (Exception ex)
            {
                string message = "There was an error parsing the Self-Diagnostics Configuration String: " + ex.Message;
                CoreEventSource.Log.SelfDiagnosticsParseError(message);
                throw new ArgumentException(message, ex);
            }

            if (keyValuePairs.ContainsKey(KeyDestination))
            {
                return keyValuePairs;
            }
            else
            {
                string message = Invariant($"Self-Diagnostics Configuration string is invalid. Missing key '{KeyDestination}'");
                CoreEventSource.Log.SelfDiagnosticsParseError(message);
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Evaluates a configuration string to determine if file logging was enabled. 
        /// </summary>
        /// <param name="configurationString">The key-value pairs from the config string.</param>
        /// <param name="directory">File directory for logging.</param>
        /// <param name="level">Log level.</param>
        /// <returns>Returns true if file has been specified in config string.</returns>
        internal static bool IsFileDiagnosticsEnabled(string configurationString, out string directory, out string level)
        {
            var keyValuePairs = ParseConfigurationString(configurationString);

            if (keyValuePairs[KeyDestination].Equals(ValueFile, StringComparison.OrdinalIgnoreCase))
            {
                TryGetValueWithDefault(keyValuePairs, key: KeyDirectory, defaultValue: DefaultDirectory, value: out directory);
                TryGetValueWithDefault(keyValuePairs, key: KeyLevel, defaultValue: DefaultLevel.ToString(), value: out level);
                return true;
            }
            else
            {
                directory = level = null;
                return false;
            }
        }

        internal static void TryGetValueWithDefault(IDictionary<string, string> dictionary, string key, string defaultValue, out string value)
        {
            if (!dictionary.TryGetValue(key, out value))
            {
                value = defaultValue;
            }
        }
    }
}
