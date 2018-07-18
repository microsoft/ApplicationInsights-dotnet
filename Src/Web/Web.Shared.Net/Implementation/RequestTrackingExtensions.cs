namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.TelemetryCorrelation;

    internal static class RequestTrackingExtensions
    {
        internal static RequestTelemetry CreateRequestTelemetryPrivate(
            this HttpContext platformContext)
        {
            if (platformContext == null)
            {
                throw new ArgumentException("platformContext");
            }

            var result = new RequestTelemetry();
            var currentActivity = Activity.Current;
            var requestContext = result.Context.Operation;

            if (currentActivity == null) 
            {
                // if there was no BeginRequest, ASP.NET HttpModule did not have a chance to set current activity (and will never do it).
                currentActivity = new Activity(ActivityHelpers.RequestActivityItemName);
                if (currentActivity.Extract(platformContext.Request.Headers))
                {
                    requestContext.ParentId = currentActivity.ParentId;
                }
                else
                {
                    if (ActivityHelpers.TryParseCustomHeaders(platformContext.Request, out var rootId, out var parentId))
                    {
                        currentActivity.SetParentId(rootId);
                        if (!string.IsNullOrEmpty(parentId))
                        {
                            requestContext.ParentId = parentId;
                        }
                    }
                    else
                    {
                        // As a first step in supporting W3C protocol in ApplicationInsights,
                        // we want to generate Activity Ids in the W3C compatible format.
                        // While .NET changes to Activity are pending, we want to ensure trace starts with W3C compatible Id
                        // as early as possible, so that everyone has a chance to upgrade and have compatibility with W3C systems once they arrive.
                        // So if there is no current Activity (i.e. there were no Request-Id header in the incoming request), we'll override ParentId on 
                        // the current Activity by the properly formatted one. This workaround should go away
                        // with W3C support on .NET https://github.com/dotnet/corefx/issues/30331
                        currentActivity.SetParentId(StringUtilities.GenerateTraceId());

                        // end of workaround
                    }
                }

                currentActivity.Start();
            }
            else
            {
                if (ActivityHelpers.IsHierarchicalRequestId(currentActivity.ParentId))
                {
                    requestContext.ParentId = currentActivity.ParentId;
                }
                else
                {
                    if (ActivityHelpers.ParentOperationIdHeaderName != null)
                    {
                        var parentId = platformContext.Request.UnvalidatedGetHeader(ActivityHelpers.ParentOperationIdHeaderName);
                        if (!string.IsNullOrEmpty(parentId))
                        {
                            requestContext.ParentId = parentId;
                        }
                    }
                }
            }

            // we have Activity.Current, we need to properly initialize request telemetry and store it in HttpContext
            if (string.IsNullOrEmpty(requestContext.Id))
            {
                requestContext.Id = currentActivity.RootId;
                foreach (var item in currentActivity.Baggage)
                {
                    result.Context.Properties[item.Key] = item.Value;
                }
            }

            result.Id = currentActivity.Id;

            // save current activity in case it will be lost - we will use it in Web.OperationCorrelationTelemetryIntitalizer
            platformContext.Items[ActivityHelpers.RequestActivityItemName] = currentActivity;

            platformContext.Items.Add(RequestTrackingConstants.RequestTelemetryItemName, result);
            WebEventSource.Log.WebTelemetryModuleRequestTelemetryCreated();

            return result;
        }

        internal static RequestTelemetry ReadOrCreateRequestTelemetryPrivate(
            this HttpContext platformContext)
        {
            if (platformContext == null)
            {
                throw new ArgumentException("platformContext");
            }

            var result = platformContext.GetRequestTelemetry() ??
                         CreateRequestTelemetryPrivate(platformContext);

            return result;
        }

        /// <summary>
        /// Creates request name on the base of HttpContext.
        /// </summary>
        /// <returns>Controller/Action for MVC or path for other cases.</returns>
        internal static string CreateRequestNamePrivate(this HttpContext platformContext)
        {
            var request = platformContext.Request;
            string name = request.UnvalidatedGetPath();

            if (request.RequestContext != null &&
                request.RequestContext.RouteData != null)
            {
                var routeValues = request.RequestContext.RouteData.Values;

                if (routeValues != null && routeValues.Count > 0)
                {
                    object controller;                    
                    routeValues.TryGetValue("controller", out controller);
                    string controllerString = (controller == null) ? string.Empty : controller.ToString();

                    if (!string.IsNullOrEmpty(controllerString))
                    {
                        object action;
                        routeValues.TryGetValue("action", out action);
                        string actionString = (action == null) ? string.Empty : action.ToString();

                        name = controllerString;
                        if (!string.IsNullOrEmpty(actionString))
                        {
                            name += "/" + actionString;
                        }
                        else
                        {
                            if (routeValues.Keys.Count > 1)
                            {
                                // We want to include arguments because in WebApi action is usually null 
                                // and action is resolved by controller, http method and number of arguments
                                var sortedKeys = routeValues.Keys
                                    .Where(key => !string.Equals(key, "controller", StringComparison.OrdinalIgnoreCase))
                                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                                    .ToArray();

                                string arguments = string.Join(@"/", sortedKeys);
                                name += " [" + arguments + "]";
                            }
                        }
                    }
                }
            }

            if (name.StartsWith("/__browserLink/requestData/", StringComparison.OrdinalIgnoreCase))
            {
                name = "/__browserLink";
            }

            name = request.HttpMethod + " " + name;

            return name;
        }
    }
}