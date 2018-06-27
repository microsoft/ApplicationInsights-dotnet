namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

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
            IEnumerable<string> result = Enumerable.Empty<string>();
            if (headers != null)
            {
                StringValues headerValues = headers[headerName];
                if (!StringValues.IsNullOrEmpty(headerValues))
                {
                    result = headerValues.SelectMany(headerValue => headerValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
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

        internal static bool ContainsRequestContextKeyValue(IHeaderDictionary headers, string keyName)
        {
            return !string.IsNullOrEmpty(GetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName));
        }

        internal static void SetRequestContextKeyValue(HttpHeaders headers, string keyName, string keyValue)
        {
            SetHeaderKeyValue(headers, RequestResponseHeaders.RequestContextHeader, keyName, keyValue);
        }

        internal static void SetRequestContextKeyValue(IHeaderDictionary headers, string keyName, string keyValue)
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

        internal static void SetHeaderKeyValue(IHeaderDictionary headers, string headerName, string keyName, string keyValue)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            headers[headerName] = new StringValues(HeadersUtilities.SetHeaderKeyValue(headers[headerName].AsEnumerable(), keyName, keyValue).ToArray());
        }

        internal static string[] SafeGetCommaSeparatedHeaderValues(IHeaderDictionary headers, string headerName, int maxLength, int maxItems)
        {
            string[] traceStateValues = headers.GetCommaSeparatedValues(headerName);

            if (traceStateValues == null)
            {
                return null;
            }

            int length = traceStateValues.Sum(p => p.Length) + traceStateValues.Length - 1; // all values and commas
            if (length <= maxLength && traceStateValues.Length <= maxItems)
            {
                return traceStateValues;
            }

            List<string> truncated;
            if (length > maxLength)
            {
                int currentLength = 0;

                truncated = traceStateValues.TakeWhile(kvp =>
                {
                    if (currentLength + kvp.Length > maxLength)
                    {
                        return false;
                    }

                    currentLength += kvp.Length + 1; // pair and comma
                    return true;
                }).ToList();
            }
            else
            {
                truncated = traceStateValues.ToList();
            }

            // if there are more than maxItems - truncate the end
            if (truncated.Count > maxItems)
            {
                return truncated.Take(maxItems).ToArray();
            }

            return truncated.ToArray();
        }
    }
}
