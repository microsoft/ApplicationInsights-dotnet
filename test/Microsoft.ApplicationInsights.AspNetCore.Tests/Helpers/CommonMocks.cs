namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class CommonMocks
    {
        public const string InstrumentationKey = "REQUIRED";
        public const string InstrumentationKeyHash = "0KNjBVW77H/AWpjTEcI7AP0atNgpasSkEll22AtqaVk=";
        public const string TestApplicationId = nameof(TestApplicationId);

        public static TelemetryClient MockTelemetryClient(Action<ITelemetry> onSendCallback)
        {
            return new TelemetryClient(new TelemetryConfiguration()
            {
                InstrumentationKey = InstrumentationKey,
                TelemetryChannel = new FakeTelemetryChannel { OnSend = onSendCallback }
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
