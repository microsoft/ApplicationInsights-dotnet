namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Globalization;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.Extensions.Logging;

    public class WebUserTelemetryInitializer : TelemetryInitializerBase
    {
        private const string WebUserCookieName = "ai_user";

        public WebUserTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
        {
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (!string.IsNullOrEmpty(telemetry.Context.User.Id))
            {
                TelemetryLogger.Instance.LogVerbose("WebUserTelemetryInitializer.OnInitializeTelemetry - telemetry.Context.Session.Id is null or empty, returning.");
                return;
            }

            if (string.IsNullOrEmpty(requestTelemetry.Context.User.Id))
            {
                UpdateRequestTelemetryFromPlatformContext(requestTelemetry, platformContext);
            }

            if (!string.IsNullOrEmpty(requestTelemetry.Context.User.Id))
            {
                telemetry.Context.User.Id = requestTelemetry.Context.User.Id;
            }
        }

        private static void UpdateRequestTelemetryFromPlatformContext(RequestTelemetry requestTelemetry, HttpContext platformContext)
        {
            if (platformContext.Request.Cookies != null && platformContext.Request.Cookies.ContainsKey(WebUserCookieName))
            {
                var userCookieValue = platformContext.Request.Cookies[WebUserCookieName];
                if (!string.IsNullOrEmpty(userCookieValue))
                {
                    var userCookieParts = ((string)userCookieValue).Split('|');
                    if (userCookieParts.Length >= 1)
                    {
                        requestTelemetry.Context.User.Id = userCookieParts[0];
                    }
                }
            }
        }
    }
}