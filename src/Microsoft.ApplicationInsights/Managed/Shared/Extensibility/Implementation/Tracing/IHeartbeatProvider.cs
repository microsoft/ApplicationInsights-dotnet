namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        void RegisterHeartbeatPayload(IHealthHeartbeatPayloadExtension payloadProvider);

        bool Initialize(TelemetryConfiguration configuration, int? delayMs = null, string allowedPayloadFields = null);
    }
}
