namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Threading.Tasks;

    /// <summary>
    /// A store for instrumentation App Ids. This makes sure we don't query the public endpoint to find an app Id for the same Ikey more than once.
    /// </summary>
    internal static class CorelationIdLookupHelper
    {
        /// <summary>
        /// Max number of component hashes to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        private const string CorrelationIdFormat = "aid-v1:{0}";

        private static ConcurrentDictionary<string, string> knownAppIds = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Retrieves the hash for a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="correlationId">AppId corresponding to the provided instrumentation key</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        public static bool TryGetAppId(string instrumentationKey, out string correlationId)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var found = knownAppIds.TryGetValue(instrumentationKey, out correlationId);

            if (!found)
            {
                // Simplistic cleanup to guard against this becoming a memory hog.
                if (knownAppIds.Keys.Count >= MAXSIZE)
                {
                    knownAppIds.Clear();
                }

                string appId;
                try
                {
                    // Ideally, we wouldn't do .Result and keep propogating the task up.
                    // But in this case - it is useless, because we are eventually getting called by things like profiler callbacks which are not async;
                    // If we don't block now, at some point up the call stack where we are hooking into an outgoing request / response, we will have to. Might as well do it here.
                    appId = FetchAppIdFromService(instrumentationKey.ToLowerInvariant()).Result;
                }
                catch(AggregateException)
                {
                    correlationId = string.Empty;
                    // Todo: log aggregate exception
                    return false;
                }

                correlationId = string.Format(CorrelationIdFormat, appId, CultureInfo.InvariantCulture);
                knownAppIds[instrumentationKey] = correlationId;
            }
            return true;
        }

        /// <summary>
        /// Computes the SHA256 hash for a given value and returns it in the form of a base64 encoded string.
        /// </summary>
        /// <param name="value">Value for which the hash is to be computed.</param>
        /// <returns>Base64 encoded hash string.</returns>
        private static async Task<string> FetchAppIdFromService(string value)
        {
            // Stub - needs to be replaced once the actual service is available.
            TaskCompletionSource<string> mockAsyncTask = new TaskCompletionSource<string>();
            mockAsyncTask.SetResult("mockAppId");

            return await mockAsyncTask.Task;
        }
    }
}
