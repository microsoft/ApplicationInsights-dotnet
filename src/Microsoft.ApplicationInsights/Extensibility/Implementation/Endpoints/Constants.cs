namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    internal static class Constants
    {
        internal const string BreezeEndpoint = "https://dc.services.visualstudio.com/";
        internal const string LiveMetricsEndpoint = "https://rt.services.visualstudio.com/";
        internal const string ProfilerEndpoint = "https://agent.azureserviceprofiler.net/";
        internal const string SnapshotEndpoint = "https://ppe.azureserviceprofiler.net/";

        internal const string BreezePrefix = "dc";
        internal const string LiveMetricsPrefix = "live";
        internal const string ProfilerPrefix = "profiler";
        internal const string SnapshotPrefix = "snapshot";
    }
}
