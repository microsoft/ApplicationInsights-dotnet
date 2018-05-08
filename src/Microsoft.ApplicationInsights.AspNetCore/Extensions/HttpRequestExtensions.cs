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
        /// <summary>
        /// Gets http request Uri from request object
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <returns>A New Uri object representing request Uri</returns>
        public static Uri GetUri(this HttpRequest request)
        {
            string unknownHostName = "UNKNOWN-HOST";

            if (null == request)
            {
                throw new ArgumentNullException("request");
            }

            if (true == string.IsNullOrWhiteSpace(request.Scheme))
            {
                throw new ArgumentException("Http request Scheme is not specified");
            }

            string hostName = request.Host.HasValue ? request.Host.ToString() : unknownHostName;
            
            var builder = new StringBuilder();

            builder.Append(request.Scheme)
                .Append("://")
                .Append(hostName);

            if (true == request.Path.HasValue)
            {
                builder.Append(request.Path.Value);
            }

            if (true == request.QueryString.HasValue)
            {
                builder.Append(request.QueryString);
            }

            return new Uri(builder.ToString());
        }
    }
}
