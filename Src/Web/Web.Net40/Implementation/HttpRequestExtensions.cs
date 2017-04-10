namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Collections.Specialized;
    using System.Web;

    /// <summary>
    /// HttpRequest extension methods.
    /// </summary>
    internal static partial class HttpRequestExtensions
    {
        public static HttpCookie UnvalidatedGetCookie(this HttpRequest httpRequest, string name)
        {
            HttpCookie value;
            try
            {
                value = httpRequest.Cookies[name];
            }
            catch (HttpRequestValidationException)
            {
                // Exception of type HttpRequestValidationException may happen when user has field validation logic
                // retry will not validate it any more so customer's applicaiton logic may be affected if code relied on this validaiton
                // This problem only surfaced when FW 4.0 Applicaiton Insights assemblies are used with applicaiton running on FW 4.5 
                value = httpRequest.Cookies[name];
            }

            return value;
        }

        public static string UnvalidatedGetHeader(this HttpRequest httpRequest, string headerName)
        {
            string result;
            try
            {
                result = httpRequest.Headers[headerName];
            }
            catch (HttpRequestValidationException)
            {
                result = httpRequest.Headers[headerName];
            }

            return result;
        }

        public static Uri UnvalidatedGetUrl(this HttpRequest httpRequest)
        {
            try
            {
                return httpRequest.Url;
            }
            catch (HttpRequestValidationException)
            {
                return httpRequest.Url;
            }
            catch (UriFormatException)
            {
                WebEventSource.Log.WebUriFormatException();
                return null;
            }
        }

        public static string UnvalidatedGetPath(this HttpRequest httpRequest)
        {
            try
            {
                return httpRequest.Path;
            }
            catch (HttpRequestValidationException)
            {
                return httpRequest.Path;
            }
        }

        public static NameValueCollection UnvalidatedGetHeaders(this HttpRequest httpRequest)
        {
            NameValueCollection result;
            try
            {
                result = httpRequest.Headers;
            }
            catch (HttpRequestValidationException)
            {
                result = httpRequest.Headers;
            }

            return result;
        }
    }
}
