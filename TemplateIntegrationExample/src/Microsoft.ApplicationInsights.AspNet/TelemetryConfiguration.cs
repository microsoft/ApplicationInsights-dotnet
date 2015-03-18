namespace Microsoft.ApplicationInsights.Extensibility
{
	using Microsoft.ApplicationInsights.Channel;
	using System;

	public class TelemetryConfiguration
    {
		private static TelemetryConfiguration singleton;
		public static TelemetryConfiguration Active
		{
			get
			{
				if (singleton == null)
				{
					singleton = new TelemetryConfiguration();
				}
				return singleton;
			}
		}

		public TelemetryChannel TelemetryChannel { get; set; }

		public string InstrumentationKey
		{
			get;
			set;
		}
	}
}