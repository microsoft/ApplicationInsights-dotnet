namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.AspNet.Builder;
	using System;

	public static class ApplicationInsightMiddlewareExtensions
    {
		public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
		{
			app.UseMiddleware<ApplicationInsightsRequestMiddleware>();
			return app;
		}

		public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
		{
			app.UseMiddleware<ApplicationInsightsExceptionMiddleware>();
			return app;
		}

		public static IApplicationBuilder SetApplicationInsightsTelemetryDeveloperMode(this IApplicationBuilder app)
		{
			//do something
			return app;
		}

	}
}