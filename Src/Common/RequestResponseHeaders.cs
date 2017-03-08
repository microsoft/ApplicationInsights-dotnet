namespace Microsoft.ApplicationInsights.Common
{
    /// <summary>
    /// Header names for requests / responses.
    /// </summary>
    internal static class RequestResponseHeaders
    {
        /// <summary>
        /// Source instrumentation header that is added by an application while making http requests and retrieved by the other application when processing incoming requests.
        /// </summary>
        public const string SourceAppIdHeader = "x-ms-request-source";

        /// <summary>
        /// Target instrumentation header that is added to the response and retrieved by the calling application when processing incoming responses.
        /// </summary>
        public const string TargetAppIdHeader = "x-ms-request-target";

        /// <summary>
        /// Standard parent Id header.
        /// </summary>
        public const string StandardParentIdHeader = "x-ms-request-id";

        /// <summary>
        /// Standard root id header.
        /// </summary>
        public const string StandardRootIdHeader = "x-ms-request-root-id";
    }
}
