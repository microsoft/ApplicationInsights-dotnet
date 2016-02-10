namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal interface IQuickPulseTelemetryProcessor : ITelemetryProcessor
    {
        void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager);

        void StopCollection();
    }
}