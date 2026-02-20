namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.Web;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// HttpRequest Extensions.
    /// </summary>
    internal static partial class HttpRequestExtensions
    {
        public static HttpCookie UnvalidatedGetCookie(this HttpRequest httpRequest, string name)
        {
            return httpRequest.Unvalidated.Cookies[name];
        }

        public static string UnvalidatedGetHeader(this HttpRequest httpRequest, string headerName)
        {
            string value = httpRequest.Unvalidated.Headers[headerName];
            return EnforceMaxLength(value, RequestTrackingConstants.RequestHeaderMaxLength);
        }

        public static Uri UnvalidatedGetUrl(this HttpRequest httpRequest)
        {
            try
            {
                return httpRequest.Unvalidated.Url;
            }
            catch (UriFormatException)
            {
                WebEventSource.Log.WebUriFormatException();
                return null;
            }
        }

        public static string UnvalidatedGetPath(this HttpRequest httpRequest)
        {
            return httpRequest.Unvalidated.Path;
        }

        public static NameValueCollection UnvalidatedGetHeaders(this HttpRequest httpRequest)
        {
            return httpRequest.Unvalidated.Headers;
        }

        public static string GetUserHostAddress(this HttpRequest httpRequest)
        {
            if (httpRequest == null)
            {
                return null;
            }

            try
            {
                return httpRequest.UserHostAddress;
            }
            catch (ArgumentException exp)
            {
                // System.ArgumentException: Value does not fall within the expected range. Fails in IIS7, WCF OneWay.
                WebEventSource.Log.UserHostNotCollectedWarning(exp.ToInvariantString());
                return null;
            }
        }

        private static string EnforceMaxLength(string input, int maxLength)
        {
            Debug.Assert(
                maxLength > 0,
                string.Format(CultureInfo.CurrentCulture, "{0} must be greater than 0", nameof(maxLength)));

            if (input != null && input.Length > maxLength)
            {
                input = input.Substring(0, maxLength);
            }

            return input;
        }
    }
}
