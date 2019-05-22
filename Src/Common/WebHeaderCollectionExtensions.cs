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

            IEnumerable<string> headerValue = headers.GetHeaderValue(headerName);
            return HeadersUtilities.GetHeaderKeyValue(headerValue, keyName);
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

            IEnumerable<string> headerValue = headers.GetHeaderValue(headerName);
            return HeadersUtilities.GetHeaderDictionary(headerValue);
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

            IEnumerable<string> headerValue = headers.GetHeaderValue(headerName);
            headers[headerName] = string.Join(",", HeadersUtilities.UpdateHeaderWithKeyValue(headerValue, keyName, value));
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

        /// <summary>
        /// For the given header collection, for a given header name, returns collection of header values.
        /// </summary>
        /// <param name="headers">Header collection.</param>
        /// <param name="headerName">Name of the header in the collection.</param>
        /// <param name="maxStringLength">Maximum allowed header length.</param>
        /// <param name="maxItems">Maximum allowed number comma separated values in the header.</param>
        /// <returns>List of comma separated values in the given header.</returns>
        public static IEnumerable<string> GetHeaderValue(this NameValueCollection headers, string headerName, int maxStringLength = -1, int maxItems = -1)
        {
            var headerValueStr = headers[headerName];
            if (headerValueStr != null)
            {
                if (maxStringLength >= 0 && headerValueStr.Length > maxStringLength)
                {
                    int lastValidComma = maxStringLength;
                    while (headerValueStr[lastValidComma] != ',' && lastValidComma > 0)
                    {
                        lastValidComma--;
                    }

                    if (lastValidComma <= 0)
                    {
                        return null;
                    }

                    headerValueStr = headerValueStr.Substring(0, lastValidComma);
                }

                var items = headerValueStr.Split(',');
                if (maxItems > 0 && items.Length > maxItems)
                {
                    return items.Take(maxItems);
                }

                return items;
            }

            return null;
        }

        private static string FormatKeyValueHeader(string key, string value)
        {
            return key.Trim() + KeyValuePairSeparator + value.Trim();
        }
    }
}
