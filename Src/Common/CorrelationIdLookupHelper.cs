namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Extensibility;
    using Extensibility.Implementation.Tracing;

    /// <summary>
    /// A store for instrumentation App Ids. This makes sure we don't query the public endpoint to find an app Id for the same instrumentation key more than once.
    /// </summary>
    internal class CorrelationIdLookupHelper
    {
        /// <summary>
        /// Max number of app ids to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        // For now we have decided to go with not waiting to retrieve the app Id, instead we just cache it on retrieval.
        // This means the initial few attempts to get correlation id might fail and the initial telemetry sent might be missing such data.
        // However, once it is in the cache - subsequent telemetry should contain this data. 
        private const int GetAppIdTimeout = 0; // milliseconds

        private const string CorrelationIdFormat = "cid-v1:{0}";

        private const string AppIdQueryApiRelativeUriFormat = "api/profiles/{0}/appId";

        private Uri endpointAddress;

        private ConcurrentDictionary<string, string> knownCorrelationIds = new ConcurrentDictionary<string, string>();

        private Func<string, Task<string>> provideAppId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdLookupHelper" /> class mostly to be used by the test classes to provide an override for fetching appId logic.
        /// </summary>
        /// <param name="appIdProviderMethod">The delegate to be called to fetch the appId.</param>
        public CorrelationIdLookupHelper(Func<string, Task<string>> appIdProviderMethod)
        {
            if (appIdProviderMethod == null)
            {
                throw new ArgumentNullException(nameof(appIdProviderMethod));
            }

            this.provideAppId = appIdProviderMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdLookupHelper" /> class.
        /// </summary>
        /// <param name="endpointAddress">Endpoint that is to be used to fetch appId.</param>
        public CorrelationIdLookupHelper(string endpointAddress)
        {
            if (string.IsNullOrEmpty(endpointAddress))
            {
                throw new ArgumentNullException(nameof(endpointAddress));
            }

            Uri endpointUri = new Uri(endpointAddress);

            // Get the base URI, so that we can append the known relative segments to it.
            this.endpointAddress = new Uri(endpointUri.AbsoluteUri.Substring(0, endpointUri.AbsoluteUri.Length - endpointUri.LocalPath.Length));

            this.provideAppId = this.FetchAppIdFromService;
        }

        /// <summary>
        /// Retrieves the correlation id corresponding to a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="correlationId">AppId corresponding to the provided instrumentation key.</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        public bool TryGetXComponentCorrelationId(string instrumentationKey, out string correlationId)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                // This method cannot throw - it's a Try... method. We cannot proceed any further.
                correlationId = string.Empty;
                return false;
            }

            var found = this.knownCorrelationIds.TryGetValue(instrumentationKey, out correlationId);

            if (found)
            {
                return true;
            }
            else
            {
                // Simplistic cleanup to guard against this becoming a memory hog.
                if (this.knownCorrelationIds.Keys.Count >= MAXSIZE)
                {
                    this.knownCorrelationIds.Clear();
                }

                try
                {
                    // Todo: When this fails, say in the vortex endpoint case, ProfileQueryEndpont is not provided, it may perpetually keep failing.
                    // We can possibly make it more robust by storing the failure and quiting querying after a few attempts.
                    // Is that worth the effort?

                    // We wait for <getAppIdTimeout> seconds (which is 0 at this point) to retrieve the appId. If retrieved during that time, we return success setting the correlation id.
                    // If we are still waiting on the result beyond the timeout - for this particular call we return the failure but queue a task continuation for it to be cached for next time.
                    Task<string> getAppIdTask = this.provideAppId(instrumentationKey.ToLowerInvariant());
                    if (getAppIdTask.Wait(GetAppIdTimeout))
                    {
                        this.GenerateCorrelationIdAndAddToDictionary(instrumentationKey, getAppIdTask.Result);
                        correlationId = this.knownCorrelationIds[instrumentationKey];
                        return true;
                    }
                    else
                    {
                        getAppIdTask.ContinueWith((appId) =>
                        {
                            try
                            {
                                this.GenerateCorrelationIdAndAddToDictionary(instrumentationKey, appId.Result);
                            }
                            catch (AggregateException ae)
                            {
                                CrossComponentCorrelationEventSource.Log.FetchAppIdFailed(ae.Flatten().InnerException.ToInvariantString());
                            }
                        });

                        return false;
                    }
                }
                catch (AggregateException ae)
                {
                    CrossComponentCorrelationEventSource.Log.FetchAppIdFailed(ae.Flatten().InnerException.ToInvariantString());

                    correlationId = string.Empty;
                    return false;
                }
            }
        }

        private void GenerateCorrelationIdAndAddToDictionary(string ikey, string appId)
        {
            this.knownCorrelationIds[ikey] = string.Format(CorrelationIdFormat, appId, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Retrieves the app id given the instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which app id is to be retrieved.</param>
        /// <returns>App id.</returns>
        private async Task<string> FetchAppIdFromService(string instrumentationKey)
        {
            try
            {
                SdkInternalOperationsMonitor.Enter();

                Uri appIdEndpoint = this.GetAppIdEndPointUri(instrumentationKey);

                WebRequest request = WebRequest.Create(appIdEndpoint);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        return await reader.ReadToEndAsync();
                    }
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
            return new Uri(this.endpointAddress, string.Format(AppIdQueryApiRelativeUriFormat, instrumentationKey, CultureInfo.InvariantCulture));
        }
    }
}
