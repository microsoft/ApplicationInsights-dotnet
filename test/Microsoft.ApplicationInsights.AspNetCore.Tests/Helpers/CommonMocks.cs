namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using System;
    using DiagnosticListeners;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class CommonMocks
    {
        public const string InstrumentationKey = "REQUIRED";
        public const string InstrumentationKeyHash = "0KNjBVW77H/AWpjTEcI7AP0atNgpasSkEll22AtqaVk=";

        public static TelemetryClient MockTelemetryClient(Action<ITelemetry> onSendCallback)
        {
            return new TelemetryClient(new TelemetryConfiguration()
            {
                InstrumentationKey = InstrumentationKey,
                TelemetryChannel = new FakeTelemetryChannel { OnSend = onSendCallback }
            });
        }

        internal static ICorrelationIdLookupHelper MockCorrelationIdLookupHelper()
        {
            return new CorrelationIdLookupHelperStub();
        }
    }
}
