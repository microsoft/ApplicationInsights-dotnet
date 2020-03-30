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
        internal const string KeyFilePath = "Directory";
        internal const string KeyLevel = "Level";
        internal const string ValueDestinationFile = "file";

        /// <summary>
        /// Parse a configuration string and return a Dictionary.
        /// </summary>
        /// <remarks>Example: "key1=value1;key2=value2;key3=value3".</remarks>
        /// <returns>A dictionary parsed from the input configuration string.</returns>
        internal static IDictionary<string, string> ParseConfigurationString(string configurationString)
        {
            var keyValuePairs = ConfigStringParser.Parse(configurationString, configName: "Self-Diagnostics Configuration String");

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
        /// Evaluates if the key-value pairs specifies writing to file. If so, parses and returns params.
        /// </summary>
        /// <param name="keyValuePairs">The key-value pairs from the config string.</param>
        /// <param name="path">File directory for logging.</param>
        /// <param name="level">Log level.</param>
        /// <returns>Returns true if file has been specified in config string.</returns>
        internal static bool IsFileDiagnostics(IDictionary<string, string> keyValuePairs, out string path, out string level)
        {
            if (keyValuePairs[KeyDestination].Equals(ValueDestinationFile, StringComparison.OrdinalIgnoreCase))
            {
                TryGetValueWithDefault(keyValuePairs, key: KeyFilePath, defaultValue: "%TEMP%", value: out path);
                TryGetValueWithDefault(keyValuePairs, key: KeyLevel, defaultValue: "Verbose", value: out level);

                return true;
            }
            else
            {
                path = level = null;
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
