namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Extensibility;

    internal class ProfileServiceWrapper : IDisposable
    {
        public ConcurrentDictionary<string, bool> FetchTasks = new ConcurrentDictionary<string, bool>();

        internal FailedRequestsManager FailedRequestsManager = new FailedRequestsManager();

        private HttpClient httpClient = new HttpClient();

        public string ProfileQueryEndpoint { get; set; }

        public async Task<string> FetchAppIdAsync(string instrumentationKey)
        {
            if (this.FailedRequestsManager.CanRetry(instrumentationKey)
                && this.FetchTasks.TryAdd(instrumentationKey, true))
            {
                try
                {
                    return await this.SendRequestAsync(instrumentationKey.ToLowerInvariant());
                }
                catch (Exception ex)
                {
                    this.FailedRequestsManager.RegisterFetchFailure(instrumentationKey, ex);
                    return null;
                }
                finally
                {
                    this.FetchTasks.TryRemove(instrumentationKey, out bool value);
                }
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <summary>Send HttpRequest to get config id.</summary>
        /// <remarks>This method is internal so it can be moq-ed in a unit test.</remarks>
        internal virtual async Task<HttpResponseMessage> GetAsync(string instrumentationKey)
        {
            Uri appIdEndpoint = this.GetAppIdEndPointUri(instrumentationKey);
            return await this.httpClient.GetAsync(appIdEndpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the AppId given the instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which AppId is to be retrieved.</param>
        /// <returns>Task to resolve AppId.</returns>
        private async Task<string> SendRequestAsync(string instrumentationKey)
        {
            try
            {
                SdkInternalOperationsMonitor.Enter();

                var resultMessage = await this.GetAsync(instrumentationKey);
                if (resultMessage.IsSuccessStatusCode)
                {
                    return await resultMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    this.FailedRequestsManager.RegisterFetchFailure(instrumentationKey, resultMessage.StatusCode);
                    return null;
                }
            }
            finally
            {
                SdkInternalOperationsMonitor.Exit();
            }
        }

        /// <summary>
        /// Strips off any relative path at the end of the base URI and then appends the known relative path to get the app id uri.
        /// </summary>
        /// <param name="instrumentationKey">AI resource's instrumentation key.</param>
        /// <returns>Computed Uri.</returns>
        private Uri GetAppIdEndPointUri(string instrumentationKey)
        {
            if (this.ProfileQueryEndpoint != null)
            {
                return new Uri(string.Format(CultureInfo.InvariantCulture, this.ProfileQueryEndpoint, instrumentationKey));
            }
            else
            {
                return new Uri(string.Format(CultureInfo.InvariantCulture, Constants.ProfileQueryEndpoint, instrumentationKey));
            }
        }
    }
}
