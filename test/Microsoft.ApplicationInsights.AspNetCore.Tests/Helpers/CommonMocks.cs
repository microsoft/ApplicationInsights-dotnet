namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class CommonMocks
    {
        public const string InstrumentationKey = "REQUIRED";
        public const string InstrumentationKeyHash = "0KNjBVW77H/AWpjTEcI7AP0atNgpasSkEll22AtqaVk=";

        public static TelemetryClient MockTelemetryClient(Action<ITelemetry> onSendCallback)
        {
            var telemetryChannel = new FakeTelemetryChannel { OnSend = onSendCallback };

            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.InstrumentationKey = InstrumentationKey;
            telemetryConfiguration.TelemetryChannel = telemetryChannel;

            return new TelemetryClient(telemetryConfiguration);
        }
    }
}
