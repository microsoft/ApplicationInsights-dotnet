namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// WebHeaderCollection extension methods.
    /// </summary>
    internal static class WebHeaderCollectionExtensions
    {
        private const string KeyValuePairSeparator = "=";

        /// <summary>
        /// For the given header collection, for a given header of name-value type, find the value of a particular key.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header in the collection.</param>
        /// <param name="keyName">Desired key of the key-value list.</param>
        /// <returns>Value against the given parameters.</returns>
        public static string GetNameValueHeaderValue(this NameValueCollection headers, string headerName, string keyName)
        {
            Debug.Assert(headerName != null, "headerName must not be null");
            Debug.Assert(keyName != null, "keyName must not be null");

            var requiredHeader = headers[headerName];

            if (requiredHeader != null)
            {
                var headerValues = requiredHeader.Split(',');
                foreach (var headerValue in headerValues)
                {
                    string key, value;
                    if (headerValue.Contains(keyName) &&
                        TryParseKeyValueHeader(headerValue, out key, out value) &&
                        keyName.Equals(key))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// For the given header collection, for a given header of name-value type, return list of KeyValuePairs.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header in the collection.</param>
        /// <returns>List of KeyValuePairs in the given header.</returns>
        public static IDictionary<string, string> GetNameValueCollectionFromHeader(this NameValueCollection headers, string headerName)
        {
            Debug.Assert(headerName != null, "headerName must not be null");

            var requiredHeader = headers[headerName];

            if (requiredHeader != null)
            {
                var values = new Dictionary<string, string>();
                var headerValues = requiredHeader.Split(',');
                foreach (var headerValue in headerValues)
                {
                    string key, value;
                    if (TryParseKeyValueHeader(headerValue, out key, out value))
                    {
                        if (!values.ContainsKey(key))
                        {
                            values.Add(key, value);
                        }
                    }
                }

                return values;
            }

            return null;
        }

        /// <summary>
        /// For the given header collection, adds KeyValuePair to header.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header that is to contain the name-value pair.</param>
        /// <param name="keyName">Name in the name value pair.</param>
        /// <param name="value">Value in the name value pair.</param>
        public static void SetNameValueHeaderValue(this NameValueCollection headers, string headerName, string keyName, string value)
        {
            Debug.Assert(headerName != null, "headerName must not be null");
            Debug.Assert(keyName != null, "keyName must not be null");

            var requiredHeader = headers[headerName];

            if (!string.IsNullOrEmpty(requiredHeader))
            {
                bool found = false;
                var headerValues = requiredHeader.Split(',');
                for (int i = 0; i < headerValues.Length; i++)
                {
                    string keyValueString = headerValues[i];
                    string currentKey, currentValue;
                    if (keyValueString.Contains(keyName) &&
                        TryParseKeyValueHeader(keyValueString, out currentKey, out currentValue) &&
                        keyName.Equals(currentKey))
                    {
                        // Overwrite the existing thing
                        headerValues[i] = FormatKeyValueHeader(keyName, value);
                        found = true;
                    }
                }

                headers[headerName] = string.Join(",", headerValues);

                if (!found)
                {
                    headers[headerName] += ',' + FormatKeyValueHeader(keyName, value);
                }
            }
            else
            {
                // header with headerName does not exist - let's add one.
                headers[headerName] = FormatKeyValueHeader(keyName, value);
            }
        }

        /// <summary>
        /// For the given header collection, sets the header value based on the name value format.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header that is to contain the name-value pair.</param>
        /// <param name="keyValuePairs">List of KeyValuePairs to format into header.</param>
        public static void SetHeaderFromNameValueCollection(this NameValueCollection headers, string headerName, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            Debug.Assert(headerName != null, "headerName must not be null");

            var requiredHeader = headers[headerName];

            // do not set header if it's present
            if (string.IsNullOrEmpty(requiredHeader))
            {
                var headerValue = string.Join(",", keyValuePairs.Select(pair => FormatKeyValueHeader(pair.Key, pair.Value)));

                if (headerValue.Length > 0)
                {
                    headers[headerName] = headerValue;
                }
            }
        }

        private static bool TryParseKeyValueHeader(string pairString, out string key, out string value)
        {
            Debug.Assert(pairString != null, "pairString is null");

            key = null;
            value = null;

            int indexOfSeparator = pairString.IndexOf(KeyValuePairSeparator, StringComparison.Ordinal);

            // Must be a separator, and cannot be first or last
            if (indexOfSeparator <= 0 || indexOfSeparator == pairString.Length - 1)
            {
                return false;
            }

            // Must be exactly one separator
            if (pairString.IndexOf(KeyValuePairSeparator, indexOfSeparator + 1, StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            key = pairString.Substring(0, indexOfSeparator).Trim();
            value = pairString.Substring(indexOfSeparator + 1).Trim();
            if (key.Length > 0 && value.Length > 0)
            {
                return true;
            }

            return false;
        }

        private static string FormatKeyValueHeader(string key, string value)
        {
            return key.Trim() + KeyValuePairSeparator + value.Trim();
        }
    }
}
