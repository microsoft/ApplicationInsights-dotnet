namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;
    using System.Threading;

    /// <summary>
    /// This class is a shim layer between our public api and the EndpointProvider.
    /// This class encapsulates caching and retrieving endpoint values.
    /// </summary>
    public class EndpointController
    {
        private readonly EndpointProvider endpointProvider = new EndpointProvider();

        private Uri breeze, liveMetrics, profiler, snapshot;
        private bool breezeInitialized, liveMetricsInitialized, profilerInitialized, snapshotInitialized;
        private object syncObject;

        /// <summary>
        /// Gets or sets the connection string (key1=value1;key2=value2;key3=value3). 
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return this.endpointProvider.ConnectionString;
            }

            set
            {
                lock (this.syncObject)
                {
                    // set new connection string and reset all the initialized booleans.
                    this.endpointProvider.ConnectionString = value;

                    this.breezeInitialized = this.liveMetricsInitialized = this.profilerInitialized = this.snapshotInitialized = false;
                }
            }
        }

        /// <summary>Gets the endpoint for Breeze (ingestion) service.</summary>
        public Uri Breeze
        {
            get { return LazyInitializer.EnsureInitialized(ref this.breeze, ref this.breezeInitialized, ref this.syncObject, () => this.endpointProvider.GetEndpoint(EndpointName.Breeze)); }
        }

        /// <summary>Gets the endpoint for Live Metrics (aka QuickPulse) service.</summary>
        public Uri LiveMetrics
        {
            get { return LazyInitializer.EnsureInitialized(ref this.liveMetrics, ref this.liveMetricsInitialized, ref this.syncObject, () => this.endpointProvider.GetEndpoint(EndpointName.LiveMetrics)); }
        }

        /// <summary>Gets the endpoint for the Profiler service.</summary>
        public Uri Profiler
        {
            get { return LazyInitializer.EnsureInitialized(ref this.profiler, ref this.profilerInitialized, ref this.syncObject, () => this.endpointProvider.GetEndpoint(EndpointName.Profiler)); }
        }

        /// <summary>Gets the endpoint for the Snapshot service.</summary>
        public Uri Snapshot
        {
            get { return LazyInitializer.EnsureInitialized(ref this.snapshot, ref this.snapshotInitialized, ref this.syncObject, () => this.endpointProvider.GetEndpoint(EndpointName.Snapshot)); }
        }
    }
}
