namespace Microsoft.ApplicationInsights.Common
{
    using System.Collections.Generic;
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;

    internal class ActivityHelpers
    {
        internal const string CorrelationContextItemName = "Microsoft.ApplicationInsights.Web.CorrelationContext";
        internal const string RequestActivityItemName = "Microsoft.ApplicationInsights.Web.Activity";

        internal static string RootOperationIdHeaderName { get; set; }

        internal static string ParentOperationIdHeaderName { get; set; }

        /// <summary> 
        /// Checks if given RequestId is hierarchical.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <returns>True if requestId is hierarchical false otherwise.</returns>
        internal static bool IsHierarchicalRequestId(string requestId)
        {
            return !string.IsNullOrEmpty(requestId) && requestId[0] == '|';
        }

        internal static bool TryParseCustomHeaders(HttpRequest request, out string rootId, out string parentId)
        {
            parentId = request.UnvalidatedGetHeader(ParentOperationIdHeaderName);
            rootId = request.UnvalidatedGetHeader(RootOperationIdHeaderName);

            if (rootId?.Length == 0)
            {
                rootId = null;
            }

            if (parentId?.Length == 0)
            {
                parentId = null;
            }

            return rootId != null || parentId != null;
        }

        private static bool TryParseStandardHeaders(HttpRequest request, out string rootId, out string parentId, out IDictionary<string, string> correlationContext)
        {
            rootId = null;
            correlationContext = null;
            parentId = request.UnvalidatedGetHeader(RequestResponseHeaders.RequestIdHeader);

            // don't bother parsing correlation-context if there was no RequestId
            if (!string.IsNullOrEmpty(parentId))
            {
                correlationContext =
                    request.UnvalidatedGetHeaders().GetNameValueCollectionFromHeader(RequestResponseHeaders.CorrelationContextHeader);

                return true;
            }

            parentId = null;
            return false;
        }
    }
}