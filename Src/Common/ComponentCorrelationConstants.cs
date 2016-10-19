namespace Microsoft.ApplicationInsights.Common
{
    /// <summary>
    /// Constants related to cross component correlation feature.
    /// </summary>
    public static class ComponentCorrelationConstants
    {
        /// <summary>
        /// Source instrumentation header that is added by an application while making http requests and retrieved by the other application when processing incoming requests.
        /// </summary>
        public const string SourceInstrumentationKeyHeader = "x-ms-request-source-ikey";

        /// <summary>
        /// Target instrumentation header that is added to the response and retrieved by the calling application when processing incoming responses.
        /// </summary>
        public const string TargetInstrumentationKeyHeader = "x-ms-request-target-ikey";
    }
}
