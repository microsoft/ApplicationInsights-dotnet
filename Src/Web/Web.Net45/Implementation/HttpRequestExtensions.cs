namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Web;

    internal static class HttpRequestExtensions
    {
        public static HttpCookie UnvalidatedGetCookie(this HttpRequest httpRequest, string name)
        {
            return httpRequest.Unvalidated.Cookies[name];
        }

        public static string UnvalidatedGetHeader(this HttpRequest httpRequest, string headerName)
        {
            return httpRequest.Unvalidated.Headers[headerName];
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
    }
}
