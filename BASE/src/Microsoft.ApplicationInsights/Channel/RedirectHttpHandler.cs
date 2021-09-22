namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal class RedirectHttpHandler : HttpClientHandler
    {
        private const int MaxRedirect = 10;

        public RedirectHttpHandler()
        {
            this.AllowAutoRedirect = false;
        }

        public Uri RedirectLocation { get; private set; } = default;

        public DateTimeOffset RedirectExpiration { get; private set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// This method will handle all requests from HttpClient.SendAsync().
        /// This inspects <see cref="HttpResponseMessage"/> to see if we received a redirect from the Ingestion service.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (DateTimeOffset.Now < this.RedirectExpiration)
            {
                // TODO: MUST BE THREAD SAFE
                request.RequestUri = this.RedirectLocation;
            }

            for (int redirects = 0; ;)
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (IsRedirection(response.StatusCode))
                {
                    if (++redirects > MaxRedirect)
                    {
                        throw new Exception("too many redirects");
                    }

                    if (this.TryGetRedirectVars(response, out Uri redirectUri))
                    {
                        request.RequestUri = this.RedirectLocation = redirectUri;
                    }
                    else
                    {
                        // cannot parse redirect headers. no action.
                        return response;
                    }
                }
                else
                {
                    return response;
                }
            }
        }

        private static bool IsRedirection(HttpStatusCode statusCode)
        {
            switch ((int)statusCode)
            {
                case 307: // StatusCodes.Status307TemporaryRedirect
                case 308: // StatusCodes.Status308PermanentRedirect
                    return true;
                default:
                    return false;
            }
        }

        private bool TryGetRedirectVars(HttpResponseMessage httpResponseMessage, out Uri redirectUri)
        {
            var cacheMaxAge = httpResponseMessage?.Headers?.CacheControl?.MaxAge;
            redirectUri = httpResponseMessage?.Headers?.Location;

            if (cacheMaxAge.HasValue && redirectUri != null && redirectUri.IsAbsoluteUri)
            {
                // TODO: MUST BE THREADSAFE
                this.RedirectLocation = redirectUri;
                this.RedirectExpiration = DateTimeOffset.Now.Add(cacheMaxAge.Value);

                return true;
            }

            return false;
        }
    }
}
