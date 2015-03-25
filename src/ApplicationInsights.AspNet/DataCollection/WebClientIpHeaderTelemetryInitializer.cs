
namespace Microsoft.ApplicationInsights.AspNet.DataCollection
{
	using Microsoft.ApplicationInsights.Extensibility;
	using System;
	using Microsoft.ApplicationInsights.Channel;
	using Microsoft.Framework.DependencyInjection;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.AspNet.Http;
	using Microsoft.AspNet.Http.Interfaces;
	using Microsoft.ApplicationInsights.AspNet.Implementation;

	public class WebClientIpHeaderTelemetryInitializer : ITelemetryInitializer
	{
		private IServiceProvider serviceProvider;

        public WebClientIpHeaderTelemetryInitializer(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
        }

		public void Initialize(ITelemetry telemetry)
		{
			var request = this.serviceProvider.GetService<RequestTelemetry>();
			if (!string.IsNullOrEmpty(request.Context.Location.Ip))
			{
				telemetry.Context.Location.Ip = request.Context.Location.Ip;
			}
			else
			{
				var context = this.serviceProvider.GetService<HttpContextHolder>().Context;

				var connectionFeature = context.GetFeature<IHttpConnectionFeature>();

				if (connectionFeature != null)
				{
					string ip = connectionFeature.RemoteIpAddress.ToString();
					request.Context.Location.Ip = ip;
					if (request != telemetry)
					{
						telemetry.Context.Location.Ip = ip;
					}
				}
			}
		}
	}
}