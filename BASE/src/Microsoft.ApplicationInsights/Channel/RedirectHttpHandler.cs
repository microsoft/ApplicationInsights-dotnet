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

        private readonly Cache<Uri> cache = new Cache<Uri>();

        public RedirectHttpHandler()
        {
            this.AllowAutoRedirect = false;
        }

        /// <summary>
        /// This method will handle all requests from HttpClient.SendAsync().
        /// This inspects <see cref="HttpResponseMessage"/> to see if we received a redirect from the Ingestion service.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.cache.TryRead(out Uri redirectUri))
            { 
                request.RequestUri = redirectUri;
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (IsRedirection(response.StatusCode))
            {
                return await this.HandleRedirectAsync(request, response, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return response;
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

        private static bool TryGetRedirectVars(HttpResponseMessage httpResponseMessage, out Uri redirectUri, out TimeSpan expire)
        {
            expire = httpResponseMessage?.Headers?.CacheControl?.MaxAge ?? default;
            redirectUri = httpResponseMessage?.Headers?.Location;

            return expire != default && redirectUri != null && redirectUri.IsAbsoluteUri;
        }

        /// <summary>
        /// This method handles redirection.
        /// This keeps extra var allocation out of the hot path.
        /// </summary>
        private async Task<HttpResponseMessage> HandleRedirectAsync(HttpRequestMessage request, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            int redirectCount = 0;

            do
            {
                if (TryGetRedirectVars(response, out Uri newRedirectUri, out TimeSpan expire))
                {
                    this.cache.Set(newRedirectUri, expire);

                    CoreEventSource.Log.IngestionRedirectInformation($"New Ingestion Endpoint: {newRedirectUri.AbsoluteUri} Expires: {expire}");
                    request.RequestUri = newRedirectUri;
                }
                else
                {
                    break;
                }

                redirectCount++;
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            while (redirectCount < MaxRedirect && IsRedirection(response.StatusCode));

            return response;
        }

        /// <summary>
        /// Simple class to encapsulate redirect cache.
        /// </summary>
        private class Cache<T>
        {
            private readonly object lockObj = new object();

            private T cachedValue = default;

            private DateTimeOffset expiration = DateTimeOffset.MinValue;

            public bool TryRead(out T cachedValue)
            {
                if (DateTimeOffset.Now < this.expiration)
                {
                    cachedValue = this.cachedValue;
                    return true;
                }
                else
                {
                    cachedValue = default;
                    return false;
                }
            }
        
            public void Set(T cachingValue, TimeSpan expire)
            {
                lock (this.lockObj)
                {
                    this.cachedValue = cachingValue;
                    this.expiration = DateTimeOffset.Now.Add(expire);
                }
            }
        }
    }
}
