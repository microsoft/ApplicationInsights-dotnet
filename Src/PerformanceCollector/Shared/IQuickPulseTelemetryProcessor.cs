namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal interface IQuickPulseTelemetryProcessor : ITelemetryProcessor
    {
        void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager, Uri serviceEndpoint);

        void StopCollection();
    }
}