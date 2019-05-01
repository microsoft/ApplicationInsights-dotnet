namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners.Implementation;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for evens specific to AspNetCore Mvc layer
    /// </summary>
    public class MvcDiagnosticsListener : IApplicationInsightDiagnosticListener
    {
        /// <inheritdoc />
        public string ListenerName { get; } = "Microsoft.AspNetCore";

        private readonly  PropertyFetcher httpContextFetcher = new PropertyFetcher("httpContext");
        private readonly PropertyFetcher routeDataFetcher = new PropertyFetcher("routeData");
        private readonly PropertyFetcher routeValuesFetcher = new PropertyFetcher("Values");

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.AspNetCore.Mvc.BeforeAction' event
        /// </summary>
        public void OnBeforeAction(HttpContext httpContext, IDictionary<string, object> routeValues)
        {
            var telemetry = httpContext.Features.Get<RequestTelemetry>();

            if (telemetry != null && string.IsNullOrEmpty(telemetry.Name))
            {
                string name = this.GetNameFromRouteContext(routeValues);

                if (!string.IsNullOrEmpty(name))
                {
                    name = httpContext.Request.Method + " " + name;
                    telemetry.Name = name;
                }
            }
        }

        private string GetNameFromRouteContext(IDictionary<string, object> routeValues)
        {
            string name = null;

            if (routeValues.Count > 0)
            {
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

        /// <inheritdoc />
        public void OnSubscribe()
        {
        }

        /// <inheritdoc />
        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                if (value.Key == "Microsoft.AspNetCore.Mvc.BeforeAction")
                {
                    var context = httpContextFetcher.Fetch(value.Value) as HttpContext;
                    var routeData = routeDataFetcher.Fetch(value.Value);
                    var routeValues = routeValuesFetcher.Fetch(routeData) as IDictionary<string, object>;

                    if (context != null && routeValues != null)
                    {
                        this.OnBeforeAction(context, routeValues);
                    }
                }
            }
            catch (Exception ex)
            {
                AspNetCoreEventSource.Instance.DiagnosticListenerWarning("MvcDiagnosticsListener", value.Key, ex.Message);
            }
        }

        /// <inheritdoc />
        public void OnError(Exception error)
        {
        }

        /// <inheritdoc />
        public void OnCompleted()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}