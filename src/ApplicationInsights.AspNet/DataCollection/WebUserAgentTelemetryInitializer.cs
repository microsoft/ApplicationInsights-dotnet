namespace Microsoft.ApplicationInsights.AspNet.DataCollection
{
	using System;
	using Microsoft.ApplicationInsights.AspNet.Implementation;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.Framework.DependencyInjection;

	/// <summary>
	/// Telemetry initializer populates user agent (telemetry.Context.User.UserAgent) for 
	/// all telemetry data items.
	/// </summary>
	public class WebUserAgentTelemetryInitializer : ITelemetryInitializer
	{
		private IServiceProvider serviceProvider;

		public WebUserAgentTelemetryInitializer(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
		}

		public void Initialize(ITelemetry telemetry)
		{
			var request = this.serviceProvider.GetService<RequestTelemetry>();
			if (string.IsNullOrEmpty(request.Context.User.UserAgent))
			{
				var context = this.serviceProvider.GetService<HttpContextHolder>().Context;
				var userAgent = context.Request.Headers["User-Agent"];
				request.Context.User.UserAgent = userAgent;
			}
			telemetry.Context.User.UserAgent = request.Context.User.UserAgent;
		}
	}
}