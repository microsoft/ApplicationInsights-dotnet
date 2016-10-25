namespace Microsoft.ApplicationInsights.Common
{
    internal static class RequestResponseHeaders
    {
        /// <summary>
        /// Constants related to cross component correlation feature.
        /// </summary>
        internal static class ComponentCorrelation
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

        /// <summary>
        /// Headers related to operation correlation.
        /// </summary>
        internal static class OperationCorrelation
        {
            /// <summary>
            /// Standard parent Id header.
            /// </summary>
            public const string StandardParentIdHeader = "x-ms-request-id";

            /// <summary>
            /// Standard root id header.
            /// </summary>
            public const string StandardRootIdHeader = "x-ms-request-root-id";
        }

        /// <summary>
        /// Headers related to quick pulse service.
        /// </summary>
        internal static class QuickPulseService
        {
            /// <summary>
            /// Subscribed header.
            /// </summary>
            public const string XMsQpsSubscribedHeaderName = "x-ms-qps-subscribed";

            /// <summary>
            /// Transmission time header.
            /// </summary>
            public const string XMsQpsTransmissionTimeHeaderName = "x-ms-qps-transmission-time";
        }
    }
}
