namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    public enum EndpointName
    {
        [EndpointMeta(ExplicitName = "IngestionEndpoint", EndpointPrefix = EndpointConstants.BreezePrefix, Default = EndpointConstants.Breeze )]
        Breeze,

        [EndpointMeta(ExplicitName = "LiveEndpoint", EndpointPrefix = EndpointConstants.LiveMetricsPrefix, Default = EndpointConstants.LiveMetrics)]
        LiveMetrics,

        [EndpointMeta(ExplicitName = "ProfilerEndpoint", EndpointPrefix = EndpointConstants.ProfilerPrefix, Default = EndpointConstants.Profiler)]
        Profiler,

        [EndpointMeta(ExplicitName = "SnapshotEndpoint", EndpointPrefix = EndpointConstants.SnapshotPrefix, Default = EndpointConstants.Snapshot)]
        Snapshot,

    }

    public class EndpointMetaAttribute : Attribute
    {
        public string ExplicitName { get; set; }
        public string EndpointPrefix { get; set; }
        public string Default { get; set; }
    }
}
