namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// A store for instrumentation App Ids. This makes sure we don't query the public endpoint to find an app Id for the same instrumentation key more than once.
    /// </summary>
    /// <remarks>
    /// This was formerly the CorrelationIdhLookupHelper defined in the WebSdk
    /// </remarks>
    public sealed class ApplicationInsightsCorrelationIdProvider : ICorrelationIdProvider, IDisposable
    {
        internal ConcurrentDictionary<string, bool> FetchTasks = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Max number of app ids to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        private readonly ProfileServiceWrapper appIdProvider;

        private ConcurrentDictionary<string, string> knownCorrelationIds = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initialize a new instance of the <see cref="ApplicationInsightsCorrelationIdProvider"/> class.
        /// </summary>
        public ApplicationInsightsCorrelationIdProvider()
        {
            this.appIdProvider = new ProfileServiceWrapper();
        }

        /// <summary>
        /// Unit Test Only! Initializes a new instance of the <see cref="ApplicationInsightsCorrelationIdProvider" /> class and accepts mocks for fetching app id.
        /// </summary>
        internal ApplicationInsightsCorrelationIdProvider(ProfileServiceWrapper profileServiceWrapper)
        {
            this.appIdProvider = profileServiceWrapper;
        }

        /// <summary>
        /// Gets or sets the endpoint that is to be used to get the application insights resource's profile (appId etc.). 
        /// </summary>
        public string ProfileQueryEndpoint
        {
            get { return this.appIdProvider.ProfileQueryEndpoint; }
            set { this.appIdProvider.ProfileQueryEndpoint = value; }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            this.appIdProvider.Dispose();
        }

        /// <summary>
        /// Retrieves the correlation id corresponding to a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="correlationId">AppId corresponding to the provided instrumentation key.</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        public bool TryGetCorrelationId(string instrumentationKey, out string correlationId)
        {
            correlationId = null;

            if (string.IsNullOrEmpty(instrumentationKey))
            {
                return false;
            }
            else if (this.knownCorrelationIds.TryGetValue(instrumentationKey, out correlationId))
            {
                return true;
            }
            else
            {
                this.FetchCorrelationId(instrumentationKey);
                return false;
            }
        }

        /// <summary>
        /// Unit Test Only! Informs tests to wait for a fetch task to complete.
        /// </summary>
        /// <returns>True if fetch task is still in progress, false otherwise.</returns>
        internal bool IsFetchAppInProgress(string ikey)
        {
            return this.FetchTasks.ContainsKey(ikey);
        }

        private void FetchCorrelationId(string instrumentationKey)
        {
            if (this.FetchTasks.TryAdd(instrumentationKey, true))
            {
                // Simplistic cleanup to guard against this becoming a memory hog.
                if (this.knownCorrelationIds.Keys.Count >= MAXSIZE)
                {
                    this.knownCorrelationIds.Clear();
                }

                // add this task to the thread pool. 
                // We don't care when it finishes, but we don't want to block the thread.
                Task.Run(() => this.appIdProvider.FetchAppIdAsync(instrumentationKey))
                    .ContinueWith((appIdTask) =>
                        {
                            this.GenerateCorrelationIdAndAddToDictionary(instrumentationKey, appIdTask.Result);

                            this.FetchTasks.TryRemove(instrumentationKey, out bool ignoreValue);
                        })
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Format and store an iKey and appId pair into the dictionary of known correlation ids.
        /// </summary>
        /// <param name="ikey">Instrumentation Key is expected to be a Guid string.</param>
        /// <param name="appId">Application Id is expected to be a Guid string. </param>
        private void GenerateCorrelationIdAndAddToDictionary(string ikey, string appId)
        {
            if (!string.IsNullOrEmpty(ikey) && !string.IsNullOrEmpty(appId))
            {
                this.knownCorrelationIds.TryAdd(ikey, CorrelationIdHelper.FormatAppId(appId));
            }
        }
    }
}
