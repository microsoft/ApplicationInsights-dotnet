namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics;
#endif
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
#if NET40
    using Microsoft.ApplicationInsights.Net40;
#endif
    using Microsoft.ApplicationInsights.Web.Implementation;

    internal class ActivityHelpers
    {
        internal const string RequestActivityItemName = "Microsoft.AppInsights.Web.Request";
        internal const string CorrelationContextItemName = "Microsoft.AppInsights.Web.CorrelationContext";

        internal static string RootOperationIdHeaderName { get; set; }

        internal static string ParentOperationIdHeaderName { get; set; }

#if NET45
        /// <summary>
        /// Parses incoming request headers: initializes Operation Context and stores it in Activity.
        /// </summary>
        /// <param name="context">HttpContext instance.</param>
        /// <returns>RequestTelemetry with OperationContext parsed from the request.</returns>
        internal static RequestTelemetry ParseRequest(HttpContext context)
        {
            RequestTelemetry requestTelemetry = new RequestTelemetry();
            var request = context.Request;
            IDictionary<string, string> correlationContext;
            string rootId, parentId;
            if (!TryParseStandardHeaders(request, out rootId, out parentId, out correlationContext))
            {
                TryParseCustomHeaders(request, out rootId, out parentId);
            }

            var requestActivity = new Activity("Microsoft.AppInsights.Web.Request");

            var effectiveParent = rootId ?? parentId;
            if (effectiveParent != null)
            {
                requestActivity.SetParentId(effectiveParent);
            }

            if (correlationContext != null)
            {
                foreach (var item in correlationContext)
                {
                    requestActivity.AddBaggage(item.Key, item.Value);
                    if (!requestTelemetry.Context.Properties.ContainsKey(item.Key))
                    {
                        requestTelemetry.Context.Properties.Add(item.Key, item.Value);
                    }
                }
            }

            requestActivity.Start();
            
            // save activity as AsyncLocal.CallContext may be lost
            context.Items[RequestActivityItemName] = requestActivity;

            // Initialize requestTelemetry Context immediately: 
            // even though it will be initialized with Base OperationCorrelationTelemetryInitializer,
            // activity may be lost in native/managed thread hops.
            requestTelemetry.Context.Operation.ParentId = parentId;
            requestTelemetry.Context.Operation.Id = requestActivity.RootId;
            requestTelemetry.Id = requestActivity.Id;

            return requestTelemetry;
        }

        /// <summary>
        /// Restores Activity if it was lost.
        /// </summary>
        /// <param name="context">HttpContext instance.</param>
        internal static void RestoreActivityIfLost(HttpContext context)
        {
            if (Activity.Current == null)
            {
                var lostActivity = context.Items[RequestActivityItemName] as Activity;
                if (lostActivity != null)
                {
                    var restoredActivity = new Activity(lostActivity.OperationName);
                    restoredActivity.SetParentId(lostActivity.Id);
                    restoredActivity.SetStartTime(lostActivity.StartTimeUtc);
                    foreach (var item in lostActivity.Baggage)
                    {
                        restoredActivity.AddBaggage(item.Key, item.Value);
                    }

                    restoredActivity.Start();
                }
            }
        }

        /// <summary>
        /// Stops root, top-most activity, created for current request.
        /// </summary>
        internal static void StopRequestActivity()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }
#else
#pragma warning disable 618
        /// <summary>
        /// Parses incoming request headers; initializes Operation Context and stores it in CallContext.
        /// </summary>
        /// <param name="context">HttpContext instance.</param>
        /// <returns>RequestTelemetry with OperationContext parsed from the request.</returns>
        internal static RequestTelemetry ParseRequest(HttpContext context)
        {
            RequestTelemetry requestTelemetry = new RequestTelemetry();
            var request = context.Request;

            IDictionary<string, string> correlationContext;
            string rootId, parentId;
            if (!TryParseStandardHeaders(request, out rootId, out parentId, out correlationContext))
            {
                TryParseCustomHeaders(request, out rootId, out parentId);
            }

            requestTelemetry.Context.Operation.ParentId = parentId;
            if (rootId != null)
            {
                requestTelemetry.Id = ApplicationInsightsActivity.GenerateRequestId(rootId);
            }
            else if (parentId != null)
            {
                requestTelemetry.Id = ApplicationInsightsActivity.GenerateRequestId(parentId);
            }
            else
            {
                requestTelemetry.Id = ApplicationInsightsActivity.GenerateRequestId();
            }

            requestTelemetry.Context.Operation.Id = ApplicationInsightsActivity.GetRootId(requestTelemetry.Id);
            if (correlationContext != null)
            {
                foreach (var item in correlationContext)
                {
                    if (!requestTelemetry.Context.Properties.ContainsKey(item.Key))
                    {
                        requestTelemetry.Context.Properties.Add(item.Key, item.Value);
                    }
                }
            }

            CorrelationHelper.SetOperationContext(requestTelemetry, correlationContext);

            // save correlation-context in case CallContext will be lost, the rest of the context is saved in requestTelemetry
            context.Items[CorrelationContextItemName] = correlationContext;

            return requestTelemetry;
        }

        /// <summary>
        /// Restores CallContext if it was lost.
        /// </summary>
        /// <param name="context">HttpContext instance.</param>
        internal static void RestoreActivityIfLost(HttpContext context)
        {
            var requestTelemetry = context.GetRequestTelemetry();
            var correlationContext = context.Items[CorrelationContextItemName] as IDictionary<string, string>;

            CorrelationHelper.SetOperationContext(requestTelemetry, correlationContext);
        }

        /// <summary>
        /// Cleans up CallContext.
        /// </summary>
        internal static void StopRequestActivity()
        {
            // it just allows to have the same API for Activity/CallContext
            CorrelationHelper.CleanOperationContext();
        }
#pragma warning restore 618
#endif

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

                bool isHierarchicalId = IsHierarchicalRequestId(parentId);

                if (correlationContext != null)
                {
                    foreach (var item in correlationContext)
                    {
                        if (!isHierarchicalId && item.Key == "Id")
                        {
                            rootId = item.Value;
                        }
                    }
                }

                return true;
            }

            parentId = null;
            return false;
        }

        private static bool IsHierarchicalRequestId(string requestId)
        {
            return !string.IsNullOrEmpty(requestId) && requestId[0] == '|';
        }

        private static bool TryParseCustomHeaders(HttpRequest request, out string rootId, out string parentId)
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
    }
}