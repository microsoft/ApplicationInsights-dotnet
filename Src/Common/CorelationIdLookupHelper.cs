namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// A store for instrumentation App Ids. This makes sure we don't query the public endpoint to find an app Id for the same Ikey more than once.
    /// </summary>
    internal static class CorelationIdLookupHelper
    {
        /// <summary>
        /// Max number of app ids to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        private const int GET_APP_ID_TIMEOUT = 2000; // milliseconds

        private const string CORELATION_ID_FORMAT = "aid-v1:{0}";

        private const string APPID_QUERY_API_RELATIVE_URI_FORMAT = "api/profiles/{0}/appId";

        private static ConcurrentDictionary<string, string> knownCorelationIds = new ConcurrentDictionary<string, string>();

        private static Func<string, string, Task<string>> provideAppId = FetchAppIdFromService;

        /// <summary>
        /// This is a test hook. Use this to provide your own test implementation for fetching the appId.
        /// </summary>
        /// <param name="appIdProviderMethod">The delegate to be called to fetch the appId</param>
        public static void OverrideAppIdProvider(Func<string, string, Task<string>> appIdProviderMethod)
        {
            provideAppId = appIdProviderMethod;
        }

        /// <summary>
        /// Retrieves the corelation id corresponding to a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="breezeEndpointAddress">Breeze endpoint to query against.</param>
        /// <param name="corelationId">AppId corresponding to the provided instrumentation key</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        public static bool TryGetXComponentCorelationId(string instrumentationKey, string breezeEndpointAddress, out string corelationId)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var found = knownCorelationIds.TryGetValue(instrumentationKey, out corelationId);

            if (found)
            {
                return true;
            }
            else
            {

                if (breezeEndpointAddress == null)
                {
                    throw new ArgumentNullException("breezeEndpointAddress");
                }

                // Simplistic cleanup to guard against this becoming a memory hog.
                if (knownCorelationIds.Keys.Count >= MAXSIZE)
                {
                    knownCorelationIds.Clear();
                }

                try
                {
                    // Todo: When this fails, say in the vortex endpoint case, ProfileQueryEndpont is not provided, it may perpetually keep failing.
                    // We can possibly make it more robust by having an exponential backoff on making a call to prod endpoint. Or store failure and never query again.
                    // Is that worth the effort?
                    
                    // We wait for 2 seconds to retrieve the appId. If retrieved during that time, we return success setting the corelation id.
                    // If we are still waiting on the result beyond the timeout - for this particular call we return the failure but queue a task continuation for it to be cached for next time.
                    Task<string> getAppIdTask = provideAppId(breezeEndpointAddress, instrumentationKey.ToLowerInvariant());
                    if (getAppIdTask.Wait(GET_APP_ID_TIMEOUT))
                    {
                        GenerateCorelationIdAndAddToDictionary(instrumentationKey, getAppIdTask.Result);
                        corelationId = knownCorelationIds[instrumentationKey];
                        return true;
                    }
                    else
                    {
                        getAppIdTask.ContinueWith((appId) =>
                        {
                            try
                            {
                                GenerateCorelationIdAndAddToDictionary(instrumentationKey, appId.Result);
                            }
                            catch (AggregateException ae)
                            {
                                CommonEventSource.Log.FetchAppIdFailed(ae.Flatten().InnerException.ToString());
                            }
                        });

                        return false;
                    }
                }
                catch(AggregateException ae)
                {
                    CommonEventSource.Log.FetchAppIdFailed(ae.Flatten().InnerException.ToString());

                    corelationId = string.Empty;
                    return false;
                }
            }
        }

        private static void GenerateCorelationIdAndAddToDictionary(string ikey, string appId)
        {
            knownCorelationIds[ikey] = string.Format(CORELATION_ID_FORMAT, appId, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Retrieves the app id given the instrumentation key
        /// </summary>
        /// <param name="breezeEndpoint">Endpoint to fetch the app Id from.</param>
        /// <param name="instrumentationKey">Instrumentation key for which app id is to be retrieved.</param>
        /// <returns>App id</returns>
        private static async Task<string> FetchAppIdFromService(string breezeEndpoint, string instrumentationKey)
        {
            Uri appIdEndpoint = GetAppIdEndPointUri(breezeEndpoint, instrumentationKey);

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
        /// <param name="breezeEndpoint">breeze / overridden endpoint uri</param>
        /// <param name="instrumentationKey">AI resoure's instrumentation key</param>
        /// <returns>Computed Uri</returns>
        private static Uri GetAppIdEndPointUri(string breezeEndpoint, string instrumentationKey)
        {
            Uri endpointUri = new Uri(breezeEndpoint);

            // Get the base URI, so that we can append the known relative segments to it.
            breezeEndpoint = endpointUri.AbsoluteUri.Substring(0, endpointUri.AbsoluteUri.Length - endpointUri.LocalPath.Length);

            return new Uri(new Uri(breezeEndpoint), string.Format(APPID_QUERY_API_RELATIVE_URI_FORMAT, instrumentationKey, CultureInfo.InvariantCulture));
        }
    }
}
