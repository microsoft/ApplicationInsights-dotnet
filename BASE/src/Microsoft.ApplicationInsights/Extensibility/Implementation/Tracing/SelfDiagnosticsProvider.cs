namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.ConfigString;

    using static System.FormattableString;

    /// <summary>
    /// This class encapsulates parsing and interpreting the self diagnostics configuration string.
    /// </summary>
    internal class SelfDiagnosticsProvider
    {
        internal const string KeyDestination = "Destination";
        internal const string KeyDirectory = "Directory";
        internal const string KeyLevel = "Level";
        internal const string ValueFile = "file";
        internal const string DefaultDirectory = "%TEMP%";
        internal const string DefaultLevel = "Verbose";

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
                throw new Exception(Invariant($"Self-Diagnostics Configuration string is invalid. Missing key '{KeyDestination}'"));
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
                TryGetValueWithDefault(keyValuePairs, key: KeyLevel, defaultValue: DefaultLevel, value: out level);
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
