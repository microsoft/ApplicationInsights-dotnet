namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    public enum EndpointName
    {
        [EndpointMeta(ExplicitName = "IngestionEndpoint", EndpointPrefix = Constants.BreezePrefix, Default = Constants.BreezeEndpoint )]
        Breeze,

        [EndpointMeta(ExplicitName = "LiveEndpoint", EndpointPrefix = Constants.LiveMetricsPrefix, Default = Constants.LiveMetricsEndpoint)]
        LiveMetrics,

        [EndpointMeta(ExplicitName = "ProfilerEndpoint", EndpointPrefix = Constants.ProfilerPrefix, Default = Constants.ProfilerEndpoint)]
        Profiler,

        [EndpointMeta(ExplicitName = "SnapshotEndpoint", EndpointPrefix = Constants.SnapshotPrefix, Default = Constants.SnapshotEndpoint)]
        Snapshot,
    }

    public class EndpointMetaAttribute : Attribute
    {
        public string ExplicitName { get; set; }
        public string EndpointPrefix { get; set; }
        public string Default { get; set; }
    }
}
