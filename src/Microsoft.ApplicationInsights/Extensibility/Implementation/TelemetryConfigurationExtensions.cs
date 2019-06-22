namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.ComponentModel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Extension methods for TelemetryConfiguration.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>
        /// Gets last known request sampling percentage to skip initializers for sampled requests
        /// </summary>
        public static double GetLastObservedSamplingPercentage(this TelemetryConfiguration configuration, SamplingTelemetryItemTypes samplingItemType)
        {
            return configuration.LastKnownSampleRateStore.GetLastObservedSamplingPercentage(samplingItemType);
        }

        /// <summary>
        /// Sets last known request sampling percentage to skip initializers for sampled requests
        /// </summary>
        public static void SetLastObservedSamplingPercentage(this TelemetryConfiguration configuration, SamplingTelemetryItemTypes samplingItemType, double value)
        {
            configuration.LastKnownSampleRateStore.SetLastObservedSamplingPercentage(samplingItemType, value);
        }
    }
}
