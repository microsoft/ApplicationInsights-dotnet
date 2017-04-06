namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Linq;
    using System.Web;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;

    internal static class RequestTrackingExtensions
    {
        internal static RequestTelemetry CreateRequestTelemetryPrivate(
            this HttpContext platformContext)
        {
            if (platformContext == null)
            {
                throw new ArgumentException("platformContext");
            }

            var result = ActivityHelpers.ParseRequest(platformContext);

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