namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// This <see cref="ApplicationInsightsCorrelationIdProvider"/> will query the Application Insights' Breeze endpoint to lookup an application id based on Instrumentation Key.
    /// This will cache lookup results to prevent repeat queries.
    /// This will rely on the <see cref="ProfileServiceWrapper" /> and <see cref="FailedRequestsManager" /> to record failed requests and block additional failing requests.
    /// </summary>
    public sealed class ApplicationInsightsCorrelationIdProvider : ICorrelationIdProvider, IDisposable
    {
        internal ConcurrentDictionary<string, bool> FetchTasks = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Max number of Application Ids to cache.
        /// </summary>
        private const int MAXSIZE = 100;

        private readonly ProfileServiceWrapper applicationIdProvider;

        private ConcurrentDictionary<string, string> knownCorrelationIds = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initialize a new instance of the <see cref="ApplicationInsightsCorrelationIdProvider"/> class.
        /// </summary>
        public ApplicationInsightsCorrelationIdProvider()
        {
            this.applicationIdProvider = new ProfileServiceWrapper();
        }

        /// <summary>
        /// Unit Test Only! Initializes a new instance of the <see cref="ApplicationInsightsCorrelationIdProvider" /> class and accepts mocks for fetching Application Id.
        /// </summary>
        internal ApplicationInsightsCorrelationIdProvider(ProfileServiceWrapper profileServiceWrapper)
        {
            this.applicationIdProvider = profileServiceWrapper;
        }

        /// <summary>
        /// Gets or sets the endpoint that is to be used to get the Application Insights resource's profile (Application Id etc.). 
        /// </summary>
        /// <remarks>TODO!! URL NEEDS TO BE BASE</remarks>
        public string ProfileQueryEndpoint
        {
            get { return this.applicationIdProvider.ProfileQueryEndpoint; }
            set { this.applicationIdProvider.ProfileQueryEndpoint = value; }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            this.applicationIdProvider.Dispose();
        }

        /// <summary>
        /// Retrieves the Correlation Id corresponding to a given Instrumentation Key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation Key string.</param>
        /// <param name="correlationId">Application Id corresponding to the provided Instrumentation Key.</param>
        /// <returns>TRUE if Correlation Id was successfully retrieved, FALSE otherwise.</returns>
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
        /// <returns>TRUE if fetch task is still in progress, FALSE otherwise.</returns>
        internal bool IsFetchAppInProgress(string instrumentationKey)
        {
            return this.FetchTasks.ContainsKey(instrumentationKey);
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
                Task.Run(() => this.applicationIdProvider.FetchApplicationIdAsync(instrumentationKey))
                    .ContinueWith((applicationIdTask) =>
                        {
                            this.GenerateCorrelationIdAndAddToDictionary(instrumentationKey, applicationIdTask.Result);

                            this.FetchTasks.TryRemove(instrumentationKey, out bool ignoreValue);
                        })
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Format and store an Instrumentation Key and Application Id pair into the dictionary of known Correlation Ids.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation Key is expected to be a Guid string.</param>
        /// <param name="applicationId">Application Id is expected to be a Guid string. </param>
        private void GenerateCorrelationIdAndAddToDictionary(string instrumentationKey, string applicationId)
        {
            if (!string.IsNullOrEmpty(instrumentationKey) && !string.IsNullOrEmpty(applicationId))
            {
                this.knownCorrelationIds.TryAdd(instrumentationKey, CorrelationIdHelper.FormatApplicationId(applicationId));
            }
        }
    }
}
