namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.AspNet.Builder;
	using Microsoft.AspNet.Http;
	using System;
	using System.Threading.Tasks;

	public class ApplicationInsightsRequestMiddleware
	{
		private readonly RequestDelegate next;
		private readonly TelemetryClient client;

		public ApplicationInsightsRequestMiddleware(RequestDelegate next, TelemetryClient client)
		{
			this.next = next;
			this.client = client;
		}

		public async Task Invoke(HttpContext context)
		{
			this.client.TrackRequest(context.Request.Path.ToString());
			await this.next(context);
		}
	}
	public class ApplicationInsightsExceptionMiddleware
	{
		private readonly RequestDelegate next;
		private readonly TelemetryClient client;

		public ApplicationInsightsExceptionMiddleware(RequestDelegate next, TelemetryClient client)
		{
			this.next = next;
			this.client = client;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await this.next(context);
			}
			catch (Exception ex)
			{
				this.client.TrackException(ex.ToString());
				throw;
			}
		}
	}
}

