namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Net;
    using Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Client-Server dependency tracking.
    /// </summary>
    internal static class WebRequestDependencyTrackingHelpers
    {
        private const string WebUserCookieName = "ai_user";
        private const string WebSessionCookieName = "ai_session";

        /// <summary>
        /// Populates WebRequest using the user, session initialized in telemetry item.
        /// </summary>
        /// <param name="dependencyTelemetry">Dependency telemetry item.</param>
        /// <param name="webRequest">Http web request.</param>
        internal static void SetUserAndSessionContextForWebRequest(DependencyTelemetry dependencyTelemetry, WebRequest webRequest)
        {
            if (dependencyTelemetry == null)
            {
                throw new ArgumentNullException("dependencyTelemetry");
            }

            if (webRequest != null)
            {
                if (dependencyTelemetry.Context.User.Id != null)
                {
                    CreateAndAddCookie(webRequest, WebUserCookieName, dependencyTelemetry.Context.User.Id);
                }

                if (dependencyTelemetry.Context.Session.Id != null)
                {
                    CreateAndAddCookie(webRequest, WebSessionCookieName, dependencyTelemetry.Context.Session.Id);
                }
            }
            else
            {
                DependencyCollectorEventSource.Log.WebRequestIsNullWarning();
            }
        }

        /// <summary>
        /// Populates WebRequest using the operation context in telemetry item.
        /// </summary>
        /// <param name="dependencyTelemetry">Dependency telemetry item.</param>
        /// <param name="webRequest">Http web request.</param>
        internal static void SetCorrelationContextForWebRequest(DependencyTelemetry dependencyTelemetry, WebRequest webRequest)
        {
            if (dependencyTelemetry == null)
            {
                throw new ArgumentNullException("dependencyTelemetry");
            }

            if (webRequest != null)
            {
                OperationContext context = dependencyTelemetry.Context.Operation;

                if (!string.IsNullOrEmpty(context.Id))
                {
                    webRequest.Headers.Add(RequestResponseHeaders.StandardRootIdHeader, context.Id);
                }

                if (!string.IsNullOrEmpty(dependencyTelemetry.Id))
                {
                    webRequest.Headers.Add(RequestResponseHeaders.StandardParentIdHeader, dependencyTelemetry.Id);
                }
            }
            else
            {
                DependencyCollectorEventSource.Log.WebRequestIsNullWarning();
            }
        }

        /// <summary>
        /// Creates and adds cookie to the web request.
        /// </summary>
        /// <param name="webRequest">Web request object.</param>
        /// <param name="key">Cookie key.</param>
        /// <param name="value">Cookie value.</param>
        private static void CreateAndAddCookie(WebRequest webRequest, string key, string value)
        {
            if (webRequest != null)
            {
                HttpWebRequest httpRequest = webRequest as HttpWebRequest;
                Cookie cookie = new Cookie(key, value) { Domain = webRequest.RequestUri.Host };
                httpRequest.CookieContainer = httpRequest.CookieContainer ?? new CookieContainer();
                httpRequest.CookieContainer.Add(cookie);
            }
            else
            {
                DependencyCollectorEventSource.Log.WebRequestIsNullWarning();
            }
        }
    }
}
