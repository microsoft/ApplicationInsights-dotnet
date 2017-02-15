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
            TelemetryClient client = new TelemetryClient(new TelemetryConfiguration()
            {
                InstrumentationKey = InstrumentationKey,
                TelemetryChannel = new FakeTelemetryChannel { OnSend = onSendCallback }
            });

            // You'd think that the constructor would set the instrumentation key is the
            // configuration had a instrumentation key, but it doesn't, so we have to set it here.
            client.InstrumentationKey = InstrumentationKey;

            return client;
        }
    }
}
