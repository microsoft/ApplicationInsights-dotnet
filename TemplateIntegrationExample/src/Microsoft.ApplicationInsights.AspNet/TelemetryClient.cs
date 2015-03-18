namespace Microsoft.ApplicationInsights
{
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using System;
	using System.Diagnostics;

	public class TelemetryClient
    {
		private TelemetryConfiguration config;

		public TelemetryClient(TelemetryConfiguration config)
		{
			this.config = config;
			this.context = new TelemetryContext();
			this.context.InstrumentationKey = config.InstrumentationKey;
			this.config.TelemetryChannel = new Channel.TelemetryChannel();
        }

		public TelemetryClient()
			: this(TelemetryConfiguration.Active)
		{
		}

		public void TrackRequest()
		{
			Debug.WriteLine("Track Request " + this.config.InstrumentationKey);
		}

		private readonly TelemetryContext context;

		public TelemetryContext Context
		{
			get
			{
				return context;
			}
		}
	}
}