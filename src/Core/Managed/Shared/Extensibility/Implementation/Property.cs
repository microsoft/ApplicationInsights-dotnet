// <copyright file="Property.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A helper class for implementing properties of telemetry and context classes.
    /// </summary>
    internal static class Property
    {
        public const int MaxDictionaryNameLength = 150;
        public const int MaxDependencyTypeLength = 1024;
        public const int MaxValueLength = 8 * 1024;
        public const int MaxEventNameLength = 512;
        public const int MaxNameLength = 1024;
        public const int MaxMessageLength = 32768;
        public const int MaxUrlLength = 2048;
        public const int MaxCommandNameLength = 2 * 1024;

        private const RegexOptions SanitizeOptions = 
#if CORE_PCL
                                                RegexOptions.None;
#else
                                                RegexOptions.Compiled;
#endif

        private static readonly Regex InvalidNameCharacters = new Regex(@"[^0-9a-zA-Z-._()\/ ]", Property.SanitizeOptions);

        public static void Set<T>(ref T property, T value) where T : class
        {
            if (value == default(T))
            {
                throw new ArgumentNullException("value");
            }

            property = value;
        }

        public static void Initialize<T>(ref T? property, T? value) where T : struct
        {
            if (!property.HasValue)
            {
                property = value;
            }
        }

        public static void Initialize(ref string property, string value)
        {
            if (string.IsNullOrEmpty(property))
            {
                property = value;
            }
        }

        public static string SanitizeEventName(this string name)
        {
            return TrimAndTruncate(name, Property.MaxEventNameLength);
        }

        public static string SanitizeName(this string name)
        {
            return TrimAndTruncate(name, Property.MaxNameLength);
        }

        public static string SanitizeDependencyType(this string value)
        {
            return TrimAndTruncate(value, Property.MaxDependencyTypeLength);
        }

        public static string SanitizeValue(this string value)
        {
            return TrimAndTruncate(value, Property.MaxValueLength);
        }

        public static string SanitizeMessage(this string message)
        {
            return TrimAndTruncate(message, Property.MaxMessageLength);
        }

        public static string SanitizeCommandName(this string message)
        {
            return TrimAndTruncate(message, Property.MaxCommandNameLength);
        }

        public static Uri SanitizeUri(this Uri uri)
        {
            if (uri != null)
            {
                string url = uri.ToString();

                if (url.Length > MaxUrlLength)
                {
                    url = url.Substring(0, MaxUrlLength);

                    // in case that the truncated string is invalid 
                    // URI we will not do nothing and let the Endpoint to drop the property
                    Uri temp;
                    if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out temp) == true)
                    {
                        uri = temp;
                    }
                }
            }

            return uri;
        }

        public static void SanitizeProperties(this IDictionary<string, string> dictionary)
        {
            if (dictionary != null)
            {
                foreach (KeyValuePair<string, string> entry in dictionary.ToArray())
                {
                    // remove the key from the dictionary first
                    dictionary.Remove(entry.Key);

                    string sanitizedKey = SanitizeKey(entry.Key, dictionary);
                    string sanitizedValue = SanitizeValue(entry.Value);

                    // add it back (sanitized at this point).
                    dictionary.Add(sanitizedKey, sanitizedValue);
                }
            }
        }

        public static void SanitizeMeasurements(this IDictionary<string, double> dictionary)
        {
            if (dictionary != null)
            {
                foreach (KeyValuePair<string, double> entry in dictionary.ToArray())
                {
                    // remove the key from the dictionary first
                    dictionary.Remove(entry.Key);

                    string sanitizedKey = SanitizeKey(entry.Key, dictionary);
                    double sanitizeValue = Utils.SanitizeNanAndInfinity(entry.Value);
                    
                    // add it back (sanitized at this point).
                    dictionary.Add(sanitizedKey, sanitizeValue);
                }
            }
        }

        private static string TrimAndTruncate(string value, int maxLength)
        {
            if (value != null)
            {
                value = value.Trim();
                value = Truncate(value, maxLength);
            }

            return value;
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        private static string SanitizeKey<TValue>(string key, IDictionary<string, TValue> dictionary)
        {
            string sanitizedKey = TrimAndTruncate(key, Property.MaxDictionaryNameLength);
            sanitizedKey = InvalidNameCharacters.Replace(sanitizedKey, "_");
            sanitizedKey = MakeKeyNonEmpty(sanitizedKey);
            sanitizedKey = MakeKeyUnique(sanitizedKey, dictionary);
            return sanitizedKey;
        }

        private static string MakeKeyNonEmpty(string key)
        {
            return string.IsNullOrEmpty(key) ? "required" : key;
        }

        private static string MakeKeyUnique<TValue>(string key, IDictionary<string, TValue> dictionary)
        {
            if (dictionary.ContainsKey(key))
            {
                const int UniqueNumberLength = 3;
                string truncatedKey = Truncate(key, MaxDictionaryNameLength - UniqueNumberLength);
                int candidate = 1;
                do
                {
                    key = truncatedKey + candidate.ToString(CultureInfo.InvariantCulture).PadLeft(UniqueNumberLength, '0');
                    candidate++;
                }
                while (dictionary.ContainsKey(key));
            }

            return key;
        }
    }
}
