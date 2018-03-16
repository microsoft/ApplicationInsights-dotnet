using System;
using System.ComponentModel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// Provides discoverable access to constants used by metric aggregates.
    /// Do not use directly. Instead, use: <c>MetricConfigurations.Common.Xxxx().Constants()</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetricConfigurationExtensions
    {
        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>. See also <c>MetricConfigurations.Common.Measurement()</c>./>
        /// </summary>
        /// <param name="measurementConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForMeasurement.AggregateKindConstants Constants(this MetricSeriesConfigurationForMeasurement measurementConfig)
        {
            return MetricSeriesConfigurationForMeasurement.AggregateKindConstants.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricConfigurationForMeasurement"/>. See also <c>MetricConfigurations.Common.Measurement()</c>./>
        /// </summary>
        /// <param name="measurementConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForMeasurement.AggregateKindConstants Constants(this MetricConfigurationForMeasurement measurementConfig)
        {
            return MetricSeriesConfigurationForMeasurement.AggregateKindConstants.Instance;
        }
    }
}
