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
            return new TelemetryClient(GetMockTelemetryConfiguration(onSendCallback));
        }

        internal static TelemetryConfiguration GetMockTelemetryConfiguration()
        {
            return new TelemetryConfiguration()
            {
                InstrumentationKey = InstrumentationKey,
                ApplicationIdProvider = new MockApplicationIdProvider(InstrumentationKey, TestApplicationId)
            };
        }

        internal static TelemetryConfiguration GetMockTelemetryConfiguration(Action<ITelemetry> onSendCallback)
        {
            return new TelemetryConfiguration()
            {
                InstrumentationKey = InstrumentationKey,
                TelemetryChannel = new FakeTelemetryChannel { OnSend = onSendCallback },
                ApplicationIdProvider = new MockApplicationIdProvider(InstrumentationKey, TestApplicationId)
            };
        }
    }
}
