namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
	using System;
	using System.Globalization;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AspNet.Hosting;
	using Microsoft.AspNet.Http;

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
				return;
			}

			if (string.IsNullOrEmpty(requestTelemetry.Context.User.Id))
			{
				var userId = GetUserIdFromPlatformContext(platformContext);
				if (!string.IsNullOrEmpty(userId))
				{
					requestTelemetry.Context.User.Id = userId;
				}
			}

			telemetry.Context.User.Id = requestTelemetry.Context.User.Id;
			if (requestTelemetry.Context.User.AcquisitionDate.HasValue)
			{
				telemetry.Context.User.AcquisitionDate = requestTelemetry.Context.User.AcquisitionDate;
			}
		}

		private static string GetUserIdFromPlatformContext(HttpContext platformContext)
		{
			if (platformContext.Request.Cookies != null && platformContext.Request.Cookies.ContainsKey(WebUserCookieName))
			{
				return platformContext.Request.Cookies[WebUserCookieName];	
			}

			return null;
		}
	}
}