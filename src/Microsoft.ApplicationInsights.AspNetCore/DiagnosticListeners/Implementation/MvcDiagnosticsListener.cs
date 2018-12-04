namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DiagnosticAdapter;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for evens specific to AspNetCore Mvc layer
    /// </summary>
    public class MvcDiagnosticsListener : IApplicationInsightDiagnosticListener
    {
        /// <inheritdoc />
        public string ListenerName { get; } = "Microsoft.AspNetCore";

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Mvc.BeforeAction' event
        /// </summary>
        [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeAction")]
        public void OnBeforeAction(HttpContext httpContext, IRouteData routeData)
        {
            var telemetry = httpContext.Features.Get<RequestTelemetry>();

            if (telemetry != null && string.IsNullOrEmpty(telemetry.Name))
            {
                string name = this.GetNameFromRouteContext(routeData);

                if (!string.IsNullOrEmpty(name))
                {
                    name = httpContext.Request.Method + " " + name;
                    telemetry.Name = name;
                }
            }
        }

        /// <inheritdoc />
        public void OnSubscribe()
        {
        }

        private string GetNameFromRouteContext(IRouteData routeData)
        {
            string name = null;

            if (routeData.Values.Count > 0)
            {
                var routeValues = routeData.Values;

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
                                !string.Equals(key, "!__route_group", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                            .ToArray();

                        if (sortedKeys.Length > 0)
                        {
                            string arguments = string.Join(@"/", sortedKeys);
                            name += " [" + arguments + "]";
                        }
                    }
                }
                else
                {
                    object page;
                    routeValues.TryGetValue("page", out page);
                    string pageString = (page == null) ? string.Empty : page.ToString();
                    if (!string.IsNullOrEmpty(pageString))
                    {
                        name = pageString;
                    }
                }
            }

            return name;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Proxy interface for <c>RouteData</c> class from Microsoft.AspNetCore.Routing.Abstractions
        /// </summary>
        public interface IRouteData
        {
            /// <summary>
            /// Gets the set of values produced by routes on the current routing path.
            /// </summary>
            IDictionary<string, object> Values { get; }
        }
    }
}