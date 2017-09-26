namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using Microsoft.ApplicationInsights.Extensibility;

    internal interface IHeartbeatProvider
    {
        void RegisterHeartbeatPayload(IHealthHeartbeatProperty payloadProvider);

        bool Initialize(TelemetryConfiguration config);

        bool UpdateSettings(TelemetryConfiguration config);
    }
}
