namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal interface IQuickPulseTelemetryProcessor : ITelemetryProcessor
    {
        void Initialize(Uri serviceEndpoint);

        void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager);

        void StopCollection();
    }
}