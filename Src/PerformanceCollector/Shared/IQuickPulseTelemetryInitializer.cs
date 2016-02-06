namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    internal interface IQuickPulseTelemetryInitializer : ITelemetryInitializer
    {
        bool Enabled { get; set; }
    }
}