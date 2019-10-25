namespace Microsoft.ApplicationInsights.Common
{
    internal class ActivityHelpers
    {
        /// <summary>
        /// Name of the item under which Activity created by request tracking (if any) will be stored
        /// It's exactly the same as one Microsoft.AspNet.TelemetryCorrelation uses
        /// https://github.com/aspnet/Microsoft.AspNet.TelemetryCorrelation/blob/6ccf0729050be4fac6797fa85af0200883db1c83/src/Microsoft.AspNet.TelemetryCorrelation/ActivityHelper.cs#L33
        /// so that TelemetryCorrelation will restore and treat 'our' Activity as it's own.
        /// </summary>
        internal const string RequestActivityItemName = "__AspnetActivity__";

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
    }
}