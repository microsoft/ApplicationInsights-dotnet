namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// This initializer sets the User Id on telemetry.
    /// </summary>
    public class WebUserTelemetryInitializer : TelemetryInitializerBase
    {
        private const string WebUserCookieName = "ai_user";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebUserTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to provide HttpContext corresponding to telemetry items.</param>
        public WebUserTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
        {
        }

        /// <inheritdoc />
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (!string.IsNullOrEmpty(telemetry.Context.User.Id))
            {
                AspNetCoreEventSource.Instance.LogWebUserTelemetryInitializerOnInitializeTelemetrySessionIdNull();
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