namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Linq;
    using System.Web;

    /// <summary>
    /// HttpContext Extensions for Activity Processors.
    /// </summary>
    internal static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the HttpRequest from the HttpContext.
        /// Returns null if no request is available (e.g., when called outside of a
        /// request context such as during <c>Application_Start</c>), in which case
        /// <see cref="HttpContext.Request"/> throws <see cref="HttpException"/>.
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <returns>The HttpRequest, or null if not available.</returns>
        public static HttpRequest GetRequest(this HttpContext context)
        {
            if (context == null)
            {
                return null;
            }

            try
            {
                return context.Request;
            }
            catch (HttpException)
            {
                // "Request is not available in this context" — e.g., Application_Start.
                return null;
            }
        }

        /// <summary>
        /// Creates a request name from the HttpContext using route data.
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <returns>The request name in format "VERB Controller/Action" or "VERB Path".</returns>
        public static string CreateRequestNamePrivate(this HttpContext context)
        {
            var request = context.GetRequest();
            if (request == null)
            {
                return string.Empty;
            }

            string verb = request.HttpMethod ?? "GET";
            
            // Try to get controller and action from route data
            var routeData = request.RequestContext?.RouteData;
            if (routeData != null && routeData.Values.Count > 0)
            {
                string controller = routeData.Values["controller"] as string;
                string action = routeData.Values["action"] as string;

                // Get parameters excluding controller and action
                var parameters = routeData.Values
                    .Where(kvp => !string.Equals(kvp.Key, "controller", StringComparison.OrdinalIgnoreCase) &&
                                  !string.Equals(kvp.Key, "action", StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Key)
                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!string.IsNullOrEmpty(controller))
                {
                    string name = verb + " " + controller;
                    
                    if (!string.IsNullOrEmpty(action))
                    {
                        name += "/" + action;
                    }
                    else if (parameters.Count > 0)
                    {
                        name += " [" + string.Join("/", parameters) + "]";
                    }
                    
                    return name;
                }
            }

            // Fallback to path
            string path = request.Unvalidated?.Path ?? request.Path;
            return verb + " " + path;
        }
    }
}
