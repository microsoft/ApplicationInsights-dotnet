namespace Microsoft.ApplicationInsights.AspNetCore
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public interface ITelemetryProcessorFactory
    {
        ITelemetryProcessor Create(ITelemetryProcessor next);
    }
}