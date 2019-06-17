namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Sampling
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;

    internal class SamplingRateStore
    {
        private readonly Dictionary<SamplingTelemetryItemTypes, double> lastKnownSampleRatePerType = new Dictionary<SamplingTelemetryItemTypes, double>
        {
            { SamplingTelemetryItemTypes.Request, 100 },
            { SamplingTelemetryItemTypes.RemoteDependency, 100 },
            { SamplingTelemetryItemTypes.Exception, 100 },
            { SamplingTelemetryItemTypes.Event, 100 },
            { SamplingTelemetryItemTypes.PageView, 100 },
            { SamplingTelemetryItemTypes.Message, 100 },
        };

        /// <summary>
        /// Gets last known request sampling percentage for telemetry type
        /// </summary>
        internal double GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes samplingItemType)
        {
            return this.lastKnownSampleRatePerType[samplingItemType];
        }

        /// <summary>
        /// Sets last known request sampling percentage for telemtry type
        /// </summary>
        internal void SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes samplingItemType, double value)
        {
            this.lastKnownSampleRatePerType[samplingItemType] = value;
        }
    }
}
