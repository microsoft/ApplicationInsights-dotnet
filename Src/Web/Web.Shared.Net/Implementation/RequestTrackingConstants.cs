namespace Microsoft.ApplicationInsights.Web.Implementation
{
    /// <summary>
    /// Request tracking constants and keys.
    /// </summary>
    internal static class RequestTrackingConstants
    {
        /// <summary>
        /// Name of the HttpContext item containing RequestTelemetry object.
        /// </summary>
        internal const string RequestTelemetryItemName = "Microsoft.ApplicationInsights.RequestTelemetry";

        internal const string EndRequestCallFlag = "Microsoft.ApplicationInsights.EndRequestCallFlag";

        /// <summary>
        /// Type name for the transfer handler. This handler is used to enable extension(less) URI 
        /// and it produces extra request, which should not be counted.
        /// </summary>
        internal const string TransferHandlerType = "System.Web.Handlers.TransferRequestHandler";

        /// <summary>
        /// The name of the cookie which holds authenticated user context information.
        /// </summary>
        internal const string WebAuthenticatedUserCookieName = "ai_authUser";
    }
}
