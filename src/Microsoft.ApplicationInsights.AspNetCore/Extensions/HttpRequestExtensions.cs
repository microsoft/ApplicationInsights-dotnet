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
        /// <param name="request">request</param>
        /// <returns>A New Uri object representing request Uri</returns>
        public static Uri GetUri(this HttpRequest request)
        {
            if (null == request)
            {
                throw new ArgumentNullException("request");
            }

            if (true == string.IsNullOrWhiteSpace(request.Scheme))
            {
                throw new ArgumentException("Http request Scheme is not specified");
            }

            if (false == request.Host.HasValue)
            {
                throw new ArgumentException("Http request Host is not specified");
            }

            var builder = new StringBuilder();

            builder.Append(request.Scheme)
                .Append("://")
                .Append(request.Host);

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
