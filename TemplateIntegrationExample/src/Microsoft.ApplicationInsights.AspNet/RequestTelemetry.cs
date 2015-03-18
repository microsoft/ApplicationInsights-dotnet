namespace Microsoft.ApplicationInsights.DataContracts
{
	using Microsoft.ApplicationInsights.DataContracts;
	using System;

	public class RequestTelemetry
    {
		public RequestTelemetry(TelemetryClient tc)
		{
			this.context = tc.Context;
		}

		private readonly TelemetryContext context;

		public TelemetryContext Context
		{
			get
			{
				return this.context;
			}
		}

    }
}