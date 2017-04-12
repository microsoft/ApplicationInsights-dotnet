namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationInsights.Common;

    /// <summary>
    /// Utilities for handling http headers.
    /// </summary>
    public static class HttpHeadersUtilities
    {
        /// <summary>
        /// Get values for given header name.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="headerName">The header name.</param>
        /// <returns>Returns a list of values the http header contains.</returns>
        public static IEnumerable<string> GetHeaderValues(HttpHeaders headers, string headerName)
        {
            IEnumerable<string> result;
            if (headers == null || !headers.TryGetValues(headerName, out result))
            {
                result = Enumerable.Empty<string>();
            }
            return result;
        }

        /// <summary>
        /// Get the value for a given key in a specific header.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="headerName">The name of the header.</param>
        /// <param name="keyName">The key for the value.</param>
        /// <returns>Returns the value for the key.</returns>
        public static string GetHeaderKeyValue(HttpHeaders headers, string headerName, string keyName)
        {
            IEnumerable<string> headerValues = GetHeaderValues(headers, headerName);
            return HeadersUtilities.GetHeaderKeyValue(headerValues, keyName);
        }

        /// <summary>
        /// Get the value for a given key in <see cref="RequestResponseHeaders.RequestContextHeader"/>.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="keyName">The key for the value.</param>
        /// <returns>Returns the value for the key.</returns>
        public static string GetRequestContextKeyValue(HttpHeaders headers, string keyName)
        {
            return GetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName);
        }

        /// <summary>
        /// Check if a specific key exists in given header by its name.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="headerName">The header name.</param>
        /// <param name="keyName">Key of the value.</param>
        /// <returns>Returns true if the key exists. Otherwise, returns false.</returns>
        public static bool ContainsHeaderKeyValue(HttpHeaders headers, string headerName, string keyName)
        {
            return !string.IsNullOrEmpty(GetHeaderKeyValue(headers, headerName, keyName));
        }

        /// <summary>
        /// Check if a specific key exists on the <see cref="RequestResponseHeaders.RequestContextHeader"/>.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="keyName">The key for the value.</param>
        /// <returns>Returns true if the key exists. Otherwise, returns false.</returns>
        public static bool ContainsRequestContextKeyValue(HttpHeaders headers, string keyName)
        {
            return ContainsHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName);
        }

        /// <summary>
        /// Set a new key value into the <see cref="RequestResponseHeaders.RequestContextHeader"/>.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="keyName">The new key.</param>
        /// <param name="keyValue">The new value.</param>
        public static void SetRequestContextKeyValue(HttpHeaders headers, string keyName, string keyValue)
        {
            SetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName, keyValue);
        }

        /// <summary>
        /// Set a new key to a given header by its name.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="headerName">The header name.</param>
        /// <param name="keyName">The new key.</param>
        /// <param name="keyValue">The new value.</param>
        public static void SetHeaderKeyValue(HttpHeaders headers, string headerName, string keyName, string keyValue)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            IEnumerable<string> headerValues = GetHeaderValues(headers, headerName);
            headers.Remove(headerName);
            headers.Add(headerName, HeadersUtilities.UpdateHeaderWithKeyValue(headerValues, keyName, keyValue));
        }
    }
}
