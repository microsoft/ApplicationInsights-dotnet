namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.Framework.ConfigurationModel;
	using Microsoft.Framework.DependencyInjection;
	using System;

	public static class ApplicationInsightsExtensions
    {
		public static void AddTelemetry(this IServiceCollection services, IConfiguration config)
		{
			TelemetryConfiguration.Active.InstrumentationKey = config.Get("ApplicationInsights:InstrumentationKey");

			services.AddInstance<TelemetryClient>(new TelemetryClient());
			services.AddScoped<RequestTelemetry>();
		}
	}
}