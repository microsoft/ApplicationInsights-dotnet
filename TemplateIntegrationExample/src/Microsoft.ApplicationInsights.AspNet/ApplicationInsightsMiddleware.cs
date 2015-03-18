namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.AspNet.Builder;
	using Microsoft.AspNet.Http;
	using System;
	using System.Threading.Tasks;

	public class ApplicationInsightsMiddleware
	{
		private readonly RequestDelegate next;
		private readonly TelemetryClient client;

		public ApplicationInsightsMiddleware(RequestDelegate next, TelemetryClient client)
		{
			this.next = next;
			this.client = client;
		}

		public async Task Invoke(HttpContext context)
		{
			this.client.TrackRequest();
			await this.next(context);
		}
	}
}

