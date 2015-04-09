namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
	using System;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AspNet.Hosting;
	using Microsoft.AspNet.Http;

	public class WebSessionTelemetryInitializer : TelemetryInitializerBase
	{
		private const string WebSessionCookieName = "ai_session";

		public WebSessionTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
             : base(httpContextAccessor)
        {
		}

		protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
		{
			if (!string.IsNullOrEmpty(telemetry.Context.Session.Id))
			{
				return;
			}

			if (string.IsNullOrEmpty(requestTelemetry.Context.Session.Id))
			{
				var userId = GetSessionIdFromPlatformContext(platformContext);
				if (!string.IsNullOrEmpty(userId))
				{
					requestTelemetry.Context.Session.Id = userId;
				}
			}

			telemetry.Context.Session.Id = requestTelemetry.Context.Session.Id;
			if (requestTelemetry.Context.Session.IsFirst.HasValue)
			{
				telemetry.Context.Session.IsFirst = requestTelemetry.Context.Session.IsFirst;
			}
		}

		private static string GetSessionIdFromPlatformContext(HttpContext platformContext)
		{
			if (platformContext.Request.Cookies != null && platformContext.Request.Cookies.ContainsKey(WebSessionCookieName))
			{
				return platformContext.Request.Cookies[WebSessionCookieName];
			}

			return null;
		}
	}
}