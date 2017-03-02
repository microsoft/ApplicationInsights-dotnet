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

        private static ConcurrentDictionary<string, string> knownCorelationIds = new ConcurrentDictionary<string, string>();

        private static Func<string, string, Task<string>> appIdProvider = FetchAppIdFromService;

        /// <summary>
        /// This is a test hook. Use this to provide your own test implementation for fetching the appId.
        /// </summary>
        /// <param name="appIdProviderMethod">The delegate to be called to fetch the appId</param>
        public static void OverrideAppIdProvider(Func<string, string, Task<string>> appIdProviderMethod)
        {
            appIdProvider = appIdProviderMethod;
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
                    // We wait for 2 seconds to retrieve the appId. If retrieved during that time, we return success setting the corelation id.
                    // If we are still waiting on the result beyond the timeout - for this particular call we return the failure but queue a task continuation for it to be cached for next time.
                    Task<string> getAppIdTask = appIdProvider(breezeEndpointAddress, instrumentationKey.ToLowerInvariant());
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
                            catch
                            {
                                // Todo: log exception
                            }
                        });

                        return false;
                    }
                }
                catch(AggregateException)
                {
                    corelationId = string.Empty;
                    // Todo: log aggregate exception
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
            WebRequest request = WebRequest.Create(breezeEndpoint);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}
