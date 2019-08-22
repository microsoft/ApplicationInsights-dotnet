namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class CommonMocks
    {
        public const string InstrumentationKey = "REQUIRED";
        public const string TestApplicationId = nameof(TestApplicationId);

        public static TelemetryClient MockTelemetryClient(Action<ITelemetry> onSendCallback, bool isW3C = true)
        {
            return new TelemetryClient(new TelemetryConfiguration()
            {
                InstrumentationKey = InstrumentationKey,
                TelemetryChannel = new FakeTelemetryChannel { OnSend = onSendCallback },
                EnableW3CCorrelation = isW3C
            });
        }

        public static TelemetryClient MockTelemetryClient(Action<ITelemetry> onSendCallback, TelemetryConfiguration configuration)
        {
            configuration.InstrumentationKey = InstrumentationKey;
            configuration.TelemetryChannel = new FakeTelemetryChannel {OnSend = onSendCallback};
            return new TelemetryClient(configuration);
        }

        internal static IApplicationIdProvider GetMockApplicationIdProvider()
        {
            return new MockApplicationIdProvider(InstrumentationKey, TestApplicationId);
        }
    }
}
