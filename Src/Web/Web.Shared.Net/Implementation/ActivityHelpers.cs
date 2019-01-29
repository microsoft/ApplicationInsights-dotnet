namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.W3C;
    using Microsoft.ApplicationInsights.W3C.Internal;
    using Microsoft.ApplicationInsights.Web.Implementation;

    internal class ActivityHelpers
    {
        internal const string RequestActivityItemName = "Microsoft.ApplicationInsights.Web.Activity";

        internal static string RootOperationIdHeaderName { get; set; }

        internal static string ParentOperationIdHeaderName { get; set; }

        internal static bool IsW3CTracingEnabled { get; set; } = false;

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
            rootId = parentId = null;
            if (ParentOperationIdHeaderName != null && RootOperationIdHeaderName != null)
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
            }

            return rootId != null || parentId != null;
        }

        internal static void ExtractW3CContext(HttpRequest request, Activity activity)
        {
            var traceParent = request.UnvalidatedGetHeader(W3CConstants.TraceParentHeader);
            if (traceParent != null)
            {
                var traceParentStr = StringUtilities.EnforceMaxLength(traceParent, InjectionGuardConstants.TraceParentHeaderMaxLength);
                activity.SetTraceparent(traceParentStr);

                if (activity.ParentId == null)
                {
                    activity.SetParentId(activity.GetTraceId());
                }
            }
            else
            {
                activity.GenerateW3CContext();
            }

            if (!activity.Baggage.Any())
            {
                var baggage = request.Headers.GetNameValueCollectionFromHeader(RequestResponseHeaders.CorrelationContextHeader);

                if (baggage != null && baggage.Any())
                {
                    foreach (var item in baggage)
                    {
                        var itemName = StringUtilities.EnforceMaxLength(item.Key, InjectionGuardConstants.ContextHeaderKeyMaxLength);
                        var itemValue = StringUtilities.EnforceMaxLength(item.Value, InjectionGuardConstants.ContextHeaderValueMaxLength);
                        activity.AddBaggage(itemName, itemValue);
                    }
                }
            }
        }

        internal static void ExtractTracestate(HttpRequest request, Activity activity, RequestTelemetry requestTelemetry)
        {
            var tracestate = request.UnvalidatedGetHeaders().GetHeaderValue(
                W3CConstants.TraceStateHeader,
                InjectionGuardConstants.TraceStateHeaderMaxLength,
                InjectionGuardConstants.TraceStateMaxPairs)?.ToList();
            if (tracestate != null && tracestate.Any())
            {
                // it's likely there are a few and string builder is not beneficial in this case
                var pairsExceptAz = new StringBuilder();
                for (int i = 0; i < tracestate.Count; i++)
                {
                    if (tracestate[i].StartsWith(W3CConstants.AzureTracestateNamespace + "=", StringComparison.Ordinal))
                    {
                        // start after 'az='
                        if (TryExtractAppIdFromAzureTracestate(tracestate[i].Substring(3), out var appId))
                        {
                            requestTelemetry.Source = appId;
                        }
                    }
                    else
                    {
                        pairsExceptAz.Append(tracestate[i]).Append(',');
                    }
                }

                if (pairsExceptAz.Length > 0)
                {
                    // remove last comma
                    var tracestateStr = pairsExceptAz.ToString(0, pairsExceptAz.Length - 1);
                    activity.SetTracestate(StringUtilities.EnforceMaxLength(tracestateStr, InjectionGuardConstants.TraceStateHeaderMaxLength));
                }
            }
        }

        private static bool TryExtractAppIdFromAzureTracestate(string azTracestate, out string appId)
        {
            appId = null;
            var parts = azTracestate.Split(W3CConstants.TracestateAzureSeparator);

            var appIds = parts.Where(p => p.StartsWith(W3CConstants.ApplicationIdTraceStateField, StringComparison.Ordinal)).ToArray();

            if (appIds.Length != 1)
            {
                return false;
            }

            appId = appIds[0];
            return true;
        }
    }
}