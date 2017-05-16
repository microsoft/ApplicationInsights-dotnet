namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
#if NET45
    using System.Diagnostics;
#endif
    using System.Linq;
    using System.Web;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
#if NET45
    using Microsoft.AspNet.TelemetryCorrelation;
#endif

    internal static class RequestTrackingExtensions
    {
        internal static RequestTelemetry CreateRequestTelemetryPrivate(
            this HttpContext platformContext)
        {
            if (platformContext == null)
            {
                throw new ArgumentException("platformContext");
            }

#if NET40
            var result = ActivityHelpers.ParseRequest(platformContext);
#else
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
                    string rootId, parentId;
                    if (ActivityHelpers.TryParseCustomHeaders(platformContext.Request, out rootId, out parentId))
                    {
                        currentActivity.SetParentId(rootId);
                        if (!string.IsNullOrEmpty(parentId))
                        {
                            requestContext.ParentId = parentId;
                        }
                    }
                    else
                    {
                        // This is workaround for the issue https://github.com/Microsoft/ApplicationInsights-dotnet/issues/538
                        // if there is no parent Activity, ID Activity generates is not random enough to work well with 
                        // ApplicationInsights sampling algorithm
                        // This code should go away when Activity is fixed: https://github.com/dotnet/corefx/issues/18418
                        currentActivity.SetParentId(result.Id);

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
                    var parentId =
                        platformContext.Request.UnvalidatedGetHeader(ActivityHelpers.ParentOperationIdHeaderName);
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        requestContext.ParentId = parentId;
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

#endif
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