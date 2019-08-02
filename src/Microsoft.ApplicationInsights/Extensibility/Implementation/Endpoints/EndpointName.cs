namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    /// <summary>
    /// These enums represent all possible endpoints within application insights infrastructure.
    /// </summary>
    public enum EndpointName
    {
        /// <summary>Breeze is the telemetr ingestion service.</summary>
        [EndpointMeta(ExplicitName = "IngestionEndpoint", EndpointPrefix = Constants.BreezePrefix, Default = Constants.BreezeEndpoint)]
        Breeze,

        /// <summary>Live Metrics aka QuickPulse service.</summary>
        [EndpointMeta(ExplicitName = "LiveEndpoint", EndpointPrefix = Constants.LiveMetricsPrefix, Default = Constants.LiveMetricsEndpoint)]
        LiveMetrics,

        /// <summary>Application Insights Profiler service.</summary>
        [EndpointMeta(ExplicitName = "ProfilerEndpoint", EndpointPrefix = Constants.ProfilerPrefix, Default = Constants.ProfilerEndpoint)]
        Profiler,

        /// <summary>Snapshot debugger service.</summary>
        [EndpointMeta(ExplicitName = "SnapshotEndpoint", EndpointPrefix = Constants.SnapshotPrefix, Default = Constants.SnapshotEndpoint)]
        Snapshot,
    }

    /// <summary>
    /// Defines meta data for possible endpoints.
    /// </summary>
    public class EndpointMetaAttribute : Attribute
    {
        /// <summary>Gets or sets the explicit name for overriding an endpoint within a connection string.</summary>
        public string ExplicitName { get; set; }

        /// <summary>Gets or sets the prefix (aka subdomain) for an endpoint.</summary>
        public string EndpointPrefix { get; set; }

        /// <summary>Gets or sets the default classic endpoint.</summary>
        public string Default { get; set; }
    }
}
