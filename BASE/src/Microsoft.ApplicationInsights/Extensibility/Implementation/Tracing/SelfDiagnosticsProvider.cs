namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.ConfigString;

    internal class SelfDiagnosticsProvider
    {
        private const string KeyType = "Destination";
        private const string KeyFilePath = "Path";
        private const string KeyFileLevel = "Level";
        private const string KeyFileMaxSize = "MaxSize";
        private const string ValueTypeFile = "file";

        /// <summary>
        /// Parse a configuration string and return a Dictionary.
        /// </summary>
        /// <remarks>Example: "key1=value1;key2=value2;key3=value3".</remarks>
        /// <returns>A dictionary parsed from the input configuration string.</returns>
        internal static Dictionary<string, string> ParseConfigurationString(string configurationString)
        {
            var keyVaulePairs = ConfigStringParser.Parse(configurationString, configName: "Self-Diagnostics Configuration String");

            if (keyVaulePairs.ContainsKey(KeyType))
            {
                return keyVaulePairs;
            }
            else
            {
                throw new Exception("Self-Diagnostics Configuration string is invalid. Missing key 'Destination'");
            }
        }

        internal static bool IsFileDiagnostics(Dictionary<string, string> keyValuePairs, out string path, out string level, out string maxSize)
        {
            if (keyValuePairs[KeyType].Equals(ValueTypeFile, StringComparison.OrdinalIgnoreCase))
            {
                TryGetValueWithDefault(keyValuePairs, key: KeyFilePath, defaultValue: "%TEMP%", value: out path);
                TryGetValueWithDefault(keyValuePairs, key: KeyFileLevel, defaultValue: "Verbose", value: out level);
                TryGetValueWithDefault(keyValuePairs, key: KeyFileMaxSize, defaultValue: "20", value: out maxSize);

                return true;
            }
            else
            {
                path = level = maxSize = null;
                return false;
            }
        }

        internal static void TryGetValueWithDefault(Dictionary<string, string> dictionary, string key, string defaultValue, out string value)
        {
            if (!dictionary.TryGetValue(key, out value))
            {
                value = defaultValue;
            }
        }
    }
}
