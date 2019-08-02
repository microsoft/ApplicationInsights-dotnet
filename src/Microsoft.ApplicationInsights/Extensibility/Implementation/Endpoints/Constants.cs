namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    /// <summary>
    /// Endpoint Constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>Default endpoint for Breeze (aka Ingestion).</summary>
        internal const string BreezeEndpoint = "https://dc.services.visualstudio.com/";

        /// <summary>Default endpoint for Live Metrics (aka QuickPulse).</summary>
        internal const string LiveMetricsEndpoint = "https://rt.services.visualstudio.com/";

        /// <summary>Default endpoint for Profiler.</summary>
        internal const string ProfilerEndpoint = "https://agent.azureserviceprofiler.net/";

        /// <summary>Default endpoint for Snapshot Debugger.</summary>
        internal const string SnapshotEndpoint = "https://ppe.azureserviceprofiler.net/";

        /// <summary>Sub-domain for Breeze endpoint. (https:// dc.applicationinsights.azure.com/).</summary>
        internal const string BreezePrefix = "dc";

        /// <summary>Sub-domain for Breeze endpoint. (https:// live.applicationinsights.azure.com/).</summary>
        internal const string LiveMetricsPrefix = "live";

        /// <summary>Sub-domain for Breeze endpoint. (https:// profiler.applicationinsights.azure.com/).</summary>
        internal const string ProfilerPrefix = "profiler";

        /// <summary>Sub-domain for Breeze endpoint. (https:// snapshot.applicationinsights.azure.com/).</summary>
        internal const string SnapshotPrefix = "snapshot";
    }
}
