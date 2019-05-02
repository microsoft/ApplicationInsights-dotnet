namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
{
    using System;
    using System.Text;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Set of extension methods for Microsoft.AspNetCore.Http.HttpRequest
    /// </summary>
    public static class HttpRequestExtensions
    {

        private const string UnknownHostName = "UNKNOWN-HOST";

        /// <summary>
        /// Gets http request Uri from request object
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <returns>A New Uri object representing request Uri</returns>
        public static Uri GetUri(this HttpRequest request)
        {            
            if (null == request)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (true == string.IsNullOrWhiteSpace(request.Scheme))
            {
                throw new ArgumentException("Http request Scheme is not specified");
            }
            
            return new Uri(string.Concat(request.Scheme,
                    "://",
                    request.Host.HasValue ? request.Host.Value : UnknownHostName,
                    request.Path.HasValue ? request.Path.Value : "",
                    request.QueryString.HasValue ? request.QueryString.Value : ""));
        }
    }
}
