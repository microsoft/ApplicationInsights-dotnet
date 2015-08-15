namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Mvc.Routing;
    using Microsoft.Framework.DependencyInjection;

    public class OperationNameTelemetryInitializer : TelemetryInitializerBase
    {
        public OperationNameTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Name))
            {
                string name = this.GetNameFromRouteContext(platformContext.RequestServices);                

                if (string.IsNullOrEmpty(name))
                {
                    name = platformContext.Request.Path.Value;
                }

                name = platformContext.Request.Method + " " + name; 
                
                var telemetryType = telemetry as RequestTelemetry;
                if (telemetryType != null && string.IsNullOrEmpty(telemetryType.Name))
                {
                    telemetryType.Name = name;
                }

                telemetry.Context.Operation.Name = name;
            }
        }

        private string GetNameFromRouteContext(IServiceProvider requestServices)
        {
            string name = null;

            if (requestServices != null)
            {
                var actionContextAccessor = requestServices.GetService<IActionContextAccessor>();

                if (actionContextAccessor != null && actionContextAccessor.ActionContext != null &&
                    actionContextAccessor.ActionContext.RouteData != null && actionContextAccessor.ActionContext.RouteData.Values.Count > 0)
                {
                    var routeValues = actionContextAccessor.ActionContext.RouteData.Values;

                    object controller;
                    routeValues.TryGetValue("controller", out controller);
                    string controllerString = (controller == null) ? string.Empty : controller.ToString();

                    if (!string.IsNullOrEmpty(controllerString))
                    {
                        name = controllerString;

                        object action;
                        routeValues.TryGetValue("action", out action);
                        string actionString = (action == null) ? string.Empty : action.ToString();
                                                
                        if (!string.IsNullOrEmpty(actionString))
                        {
                            name += "/" + actionString;
                        }

                        if (routeValues.Keys.Count > 2)
                        {
                            // Add parameters
                            var sortedKeys = routeValues.Keys
                                .Where(key => 
                                    !string.Equals(key, "controller", StringComparison.OrdinalIgnoreCase) && 
                                    !string.Equals(key, "action", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(key, AttributeRouting.RouteGroupKey, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                                .ToArray();

                            if (sortedKeys.Length > 0)
                            {
                                string arguments = string.Join(@"/", sortedKeys);
                                name += " [" + arguments + "]";
                            }
                        }
                    }
                }
            }

            return name;
        }
    }
}