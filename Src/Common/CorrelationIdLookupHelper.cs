namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Extensibility.Implementation.Tracing;
    /// <summary>
    /// A store for instrumentation App Ids. This makes sure we don't query the public endpoint to find an app Id for the same Ikey more than once.
    /// </summary>
    internal class CorrelationIdLookupHelper
    {
        /// <summary>
        /// Max number of app ids to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        private const int GET_APP_ID_TIMEOUT = 2000; // milliseconds

        private const string CORRELATION_ID_FORMAT = "aid-v1:{0}";

        private const string APPID_QUERY_API_RELATIVE_URI_FORMAT = "api/profiles/{0}/appId";

        private Uri endpointAddress;

        private ConcurrentDictionary<string, string> knownCorrelationIds = new ConcurrentDictionary<string, string>();

        private Func<string, Task<string>> provideAppId;

        /// <summary>
        /// This constructor is mostly used by the test classes to provide an override for fetching appId logic
        /// </summary>
        /// <param name="appIdProviderMethod">The delegate to be called to fetch the appId</param>
        public CorrelationIdLookupHelper(Func<string, Task<string>> appIdProviderMethod)
        {
            if (appIdProviderMethod == null)
            {
                throw new ArgumentNullException(nameof(appIdProviderMethod));
            }

            this.provideAppId = appIdProviderMethod;
        }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="endpointAddress">Endpoint that is to be used to fetch appId</param>
        public CorrelationIdLookupHelper(string endpointAddress)
        {
            if (string.IsNullOrEmpty(endpointAddress))
            {
                throw new ArgumentNullException(nameof(endpointAddress));
            }

            Uri endpointUri = new Uri(endpointAddress);

            // Get the base URI, so that we can append the known relative segments to it.
            this.endpointAddress = new Uri (endpointUri.AbsoluteUri.Substring(0, endpointUri.AbsoluteUri.Length - endpointUri.LocalPath.Length));

            this.provideAppId = FetchAppIdFromService;
        }

        /// <summary>
        /// Retrieves the correlation id corresponding to a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="correlationId">AppId corresponding to the provided instrumentation key</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        public bool TryGetXComponentCorrelationId(string instrumentationKey, out string correlationId)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                // This method cannot throw - it's a Try... method. We cannot proceed any further.
                correlationId = string.Empty;
                return false;
            }

            var found = knownCorrelationIds.TryGetValue(instrumentationKey, out correlationId);

            if (found)
            {
                return true;
            }
            else
            {

                // Simplistic cleanup to guard against this becoming a memory hog.
                if (knownCorrelationIds.Keys.Count >= MAXSIZE)
                {
                    knownCorrelationIds.Clear();
                }

                try
                {
                    // Todo: When this fails, say in the vortex endpoint case, ProfileQueryEndpont is not provided, it may perpetually keep failing.
                    // We can possibly make it more robust by having an exponential backoff on making a call to prod endpoint. Or store failure and never query again.
                    // Is that worth the effort?
                    
                    // We wait for 2 seconds to retrieve the appId. If retrieved during that time, we return success setting the correlation id.
                    // If we are still waiting on the result beyond the timeout - for this particular call we return the failure but queue a task continuation for it to be cached for next time.
                    Task<string> getAppIdTask = provideAppId(instrumentationKey.ToLowerInvariant());
                    if (getAppIdTask.Wait(GET_APP_ID_TIMEOUT))
                    {
                        GenerateCorrelationIdAndAddToDictionary(instrumentationKey, getAppIdTask.Result);
                        correlationId = knownCorrelationIds[instrumentationKey];
                        return true;
                    }
                    else
                    {
                        getAppIdTask.ContinueWith((appId) =>
                        {
                            try
                            {
                                GenerateCorrelationIdAndAddToDictionary(instrumentationKey, appId.Result);
                            }
                            catch (AggregateException ae)
                            {
                                CrossComponentCorrelationEventSource.Log.FetchAppIdFailed(ae.Flatten().InnerException.ToInvariantString());
                            }
                        });

                        return false;
                    }
                }
                catch(AggregateException ae)
                {
                    CrossComponentCorrelationEventSource.Log.FetchAppIdFailed(ae.Flatten().InnerException.ToInvariantString());

                    correlationId = string.Empty;
                    return false;
                }
            }
        }

        private void GenerateCorrelationIdAndAddToDictionary(string ikey, string appId)
        {
            knownCorrelationIds[ikey] = string.Format(CORRELATION_ID_FORMAT, appId, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Retrieves the app id given the instrumentation key
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which app id is to be retrieved.</param>
        /// <returns>App id</returns>
        private async Task<string> FetchAppIdFromService(string instrumentationKey)
        {
            Uri appIdEndpoint = GetAppIdEndPointUri(instrumentationKey);

            WebRequest request = WebRequest.Create(appIdEndpoint);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync().ConfigureAwait(false))
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        /// <summary>
        /// Strips off any relative path at the end of the base URI and then appends the known relative path to get the app id uri.
        /// </summary>
        /// <param name="instrumentationKey">AI resoure's instrumentation key</param>
        /// <returns>Computed Uri</returns>
        private Uri GetAppIdEndPointUri(string instrumentationKey)
        {
            return new Uri(this.endpointAddress, string.Format(APPID_QUERY_API_RELATIVE_URI_FORMAT, instrumentationKey, CultureInfo.InvariantCulture));
        }
    }
}
