namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    /// <summary>
    /// This class encapsulates the endpoint values.
    /// </summary>
    public class EndpointContainer
    {
        internal EndpointContainer(IEndpointProvider endpointProvider)
        {
            this.Ingestion = endpointProvider.GetEndpoint(EndpointName.Ingestion);
            this.Live = endpointProvider.GetEndpoint(EndpointName.Live);
            this.Profiler = endpointProvider.GetEndpoint(EndpointName.Profiler);
            this.Snapshot = endpointProvider.GetEndpoint(EndpointName.Snapshot);
        }

        /// <summary>Gets the endpoint for the Ingestion (aka Breeze) service.</summary>
        public Uri Ingestion { get; private set; }

        /// <summary>Gets the endpoint for Live Metrics (aka QuickPulse) service.</summary>
        public Uri Live { get; private set; }

        /// <summary>Gets the endpoint for the Profiler service.</summary>
        public Uri Profiler { get; private set; }

        /// <summary>Gets the endpoint for the Snapshot service.</summary>
        public Uri Snapshot { get; private set; }
    }
}
