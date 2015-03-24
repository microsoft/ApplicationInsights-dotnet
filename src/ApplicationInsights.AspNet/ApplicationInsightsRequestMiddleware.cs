namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.ApplicationInsights;
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.AspNet.Builder;
	using Microsoft.AspNet.Hosting;
	using Microsoft.AspNet.Http;
	using Microsoft.AspNet.RequestContainer;
	using Microsoft.Framework.ConfigurationModel;
	using Microsoft.Framework.DependencyInjection;
	using Microsoft.Framework.Logging;
	using System;
	using System.Diagnostics;
	using System.Threading.Tasks;

	public sealed class ApplicationInsightsRequestMiddleware
	{
		private readonly RequestDelegate next;
		private readonly TelemetryClient telemetryClient;

		public ApplicationInsightsRequestMiddleware(RequestDelegate next, TelemetryClient client)
		{
			this.telemetryClient = client;
			this.next = next;
		}

		public async Task Invoke(HttpContext httpContext)
		{
			var sw = new Stopwatch();
			sw.Start();

			var now = DateTimeOffset.UtcNow;

			try
			{
				await this.next.Invoke(httpContext);
			}
			finally
			{
				if (this.telemetryClient != null)
				{
					sw.Stop();

					var telemetry = new RequestTelemetry(
							httpContext.Request.Method + " " + httpContext.Request.Path.Value,
							now,
							sw.Elapsed,
							httpContext.Response.StatusCode.ToString(),
							httpContext.Response.StatusCode < 400);

					this.telemetryClient.TrackRequest(telemetry);
				}
			}
		}
	}
}

