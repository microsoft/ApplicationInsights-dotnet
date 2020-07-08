namespace Microsoft.ApplicationInsights.Extensibility.Implementation.ConfigString
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    using static System.FormattableString;

    /// <summary>
    /// Helper class to parse a configuration string.
    /// A configuration string is defined as a string composed of key/value pairs.
    /// (ex: "key1=value1;key2=value2;key3=value3").
    /// </summary>
    internal static class ConfigStringParser
    {
        private static readonly char[] SplitSemicolon = new char[] { ';' };

        /// <summary>
        /// A configuration string is defined as a string composed of key/value pairs.
        /// (ex: "key1=value1;key2=value2;key3=value3").
        /// </summary>
        /// <param name="configString">Input string to be parsed. This string cannot be null or empty. This string will be checked for validity.</param>
        /// <param name="configName">Name of the config string that is being parsed. This is used for error messages.</param>
        /// <returns>Returns a dictionary of Key/Value pairs. Keys are not case sensitive.</returns>
        /// <remarks>This is used by both Connection Strings and Self-Diagnostics configuration.</remarks>
        public static IDictionary<string, string> Parse(string configString, string configName)
        {
            if (configString == null)
            {
                string message = Invariant($"{configName} cannot be null.");
                CoreEventSource.Log.ConfigurationStringError(message);
                throw new ArgumentNullException(nameof(configString), message);
            }

            var keyValuePairs = configString.Split(SplitSemicolon, StringSplitOptions.RemoveEmptyEntries);

            if (keyValuePairs.Length == 0)
            {
                string message = Invariant($"{configName} cannot be empty.");
                CoreEventSource.Log.ConfigurationStringError(message);
                throw new ArgumentException(message, nameof(configString));
            }

            var dictionary = new Dictionary<string, string>(keyValuePairs.Length, StringComparer.OrdinalIgnoreCase);

            foreach (var pair in keyValuePairs)
            {
                var keyAndValue = pair.Split('=');
                if (keyAndValue.Length != 2)
                {
                    string message = Invariant($"{configName} contains invalid delimiters and cannot be parsed.");
                    CoreEventSource.Log.ConfigurationStringError(message);
                    throw new ArgumentException(Invariant($"{configName} Invalid: Unexpected delimiter can not be parsed. Expected: 'key1=value1;key2=value2;key3=value3'"));
                }

                var key = keyAndValue[0].Trim();
                var value = keyAndValue[1].Trim();

                if (dictionary.ContainsKey(key))
                {
                    string message = Invariant($"{configName} cannot contain duplicate keys.");
                    CoreEventSource.Log.ConfigurationStringError(message);
                    throw new ArgumentException(Invariant($"{configName} Invalid: Contains duplicate key: '{key}'."));
                }

                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }
}