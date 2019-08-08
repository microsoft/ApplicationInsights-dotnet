namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    /// <summary>
    /// These enums represent all possible endpoints within application insights infrastructure.
    /// </summary>
    internal enum EndpointName
    {
        /// <summary>Breeze is the telemetry ingestion service.</summary>
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
}
