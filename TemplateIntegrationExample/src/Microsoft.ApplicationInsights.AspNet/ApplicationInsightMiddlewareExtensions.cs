namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.AspNet.Builder;
	using System;

	public static class ApplicationInsightMiddlewareExtensions
    {
		public static IApplicationBuilder UseRequestTelemetry(this IApplicationBuilder app)
		{
			app.UseMiddleware<ApplicationInsightsMiddleware>();
			return app;
		}

		public static IApplicationBuilder UseExceptionsTelemetry(this IApplicationBuilder app)
		{
			app.UseMiddleware<ApplicationInsightsMiddleware>();
			return app;
		}

		public static IApplicationBuilder SetTelemetryDeveloperMode(this IApplicationBuilder app)
		{
			//do something
			return app;
		}

	}
}