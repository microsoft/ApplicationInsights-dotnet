namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if NET451
#endif
    using System.Net.Http.Headers;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;
#if NETCORE
    using Microsoft.ApplicationInsights.Common;
#endif
    internal static class HttpHeadersUtilities
    {
        internal static IEnumerable<string> GetHeaderValues(HttpHeaders headers, string headerName)
        {
            IEnumerable<string> result;
            if (headers == null || !headers.TryGetValues(headerName, out result))
            {
                result = Enumerable.Empty<string>();
            }
            return result;
        }

        internal static IEnumerable<string> GetHeaderValues(IHeaderDictionary headers, string headerName)
        {
            IEnumerable<string> result;
            if (headers == null)
            {
                result = Enumerable.Empty<string>();
            }
            else
            {
                var headerValues = headers[headerName];
                if (StringValues.IsNullOrEmpty(headerValues))
                {
                    result = Enumerable.Empty<string>();
                }
                else
                {
                    result = headerValues.AsEnumerable();
                }
            }
            return result;
        }

        internal static string GetHeaderKeyValue(HttpHeaders headers, string headerName, string keyName)
        {
            IEnumerable<string> headerValues = GetHeaderValues(headers, headerName);
            return HeadersUtilities.GetHeaderKeyValue(headerValues, keyName);
        }

        internal static string GetHeaderKeyValue(IHeaderDictionary headers, string headerName, string keyName)
        {
            IEnumerable<string> headerValues = GetHeaderValues(headers, headerName);
            return HeadersUtilities.GetHeaderKeyValue(headerValues, keyName);
        }

        internal static string GetRequestContextKeyValue(HttpHeaders headers, string keyName)
        {
            return GetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName);
        }

        internal static string GetRequestContextKeyValue(IHeaderDictionary headers, string keyName)
        {
            return GetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName);
        }

        internal static bool ContainsRequestContextKeyValue(HttpHeaders headers, string keyName)
        {
            return !string.IsNullOrEmpty(GetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName));
        }

        internal static void SetRequestContextKeyValue(HttpHeaders headers, string keyName, string keyValue)
        {
            SetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName, keyValue);
        }

        internal static void SetHeaderKeyValue(HttpHeaders headers, string headerName, string keyName, string keyValue)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            IEnumerable<string> headerValues = GetHeaderValues(headers, headerName);
            headers.Remove(headerName);
            headers.Add(headerName, HeadersUtilities.SetHeaderKeyValue(headerValues, keyName, keyValue));
        }
    }
}
