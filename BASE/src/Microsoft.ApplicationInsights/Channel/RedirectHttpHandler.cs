namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class RedirectHttpHandler : HttpClientHandler
    {
        internal const int MaxRedirect = 10;

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
                // TODO: MUST BE THREAD SAFE. Consider MemoryCache
                request.RequestUri = this.RedirectLocation;
            }

            HttpResponseMessage response = null;

            for (int redirects = 0; redirects <= MaxRedirect; redirects++)
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                
                if (IsRedirection(response.StatusCode) && TryGetRedirectVars(response, out Uri redirectUri, out TimeSpan cache))
                {
                    // TODO: MUST BE THREAD SAFE. Consider MemoryCache
                    this.RedirectLocation = redirectUri;
                    this.RedirectExpiration = DateTimeOffset.Now.Add(cache);

                    CoreEventSource.Log.IngestionRedirectInformation($"New Ingestion Endpoint: {redirectUri.AbsoluteUri} Expires: {cache}");
                    request.RequestUri = redirectUri;
                }
                else
                {
                    break;
                }
            }

            return response;
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

        private static bool TryGetRedirectVars(HttpResponseMessage httpResponseMessage, out Uri redirectUri, out TimeSpan cache)
        {
            cache = httpResponseMessage?.Headers?.CacheControl?.MaxAge ?? default;
            redirectUri = httpResponseMessage?.Headers?.Location;

            return cache != default && redirectUri != null && redirectUri.IsAbsoluteUri;
        }
    }
}
