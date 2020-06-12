namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal interface IQuickPulseTelemetryProcessor
    {
        Uri ServiceEndpoint { get; set; }

        void StartCollection(IQuickPulseDataAccumulatorManager accumulatorManager, Uri serviceEndpoint, TelemetryConfiguration configuration, bool disableFullTelemetryItems = false);

        void StopCollection();
    }
}