using System;
using System.ComponentModel;

using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Provides the default Metric Configuration for Measurements.
    /// Do not use directly. Instead, use: <c>MetricConfigurations.Common.Xxxx()</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetricConfigurationsCoreExtensions
    {
        private const int DefaultSeriesCountLimit = 1000;
        private const int DefaultValuesPerDimensionLimit = 100;

        private static MetricConfiguration s_defaultConfigForMeasurement = new MetricConfiguration(
                                                        DefaultSeriesCountLimit,
                                                        DefaultValuesPerDimensionLimit,
                                                        new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

        /// <summary>
        /// <para>Use to measure attributes and/or counts of items. Also, use to measure attributes and/or rates of events.<br />
        /// Produces aggregates that contain simple statistics about tracked values per time period: Count, Sum, Min, Max.<br />
        /// (This is the most commonly used metric configuration and is the default unless otherwise specified.)</para>
        /// 
        /// <para>For example, use this metric configuration to measure:<br />
        /// Size and number of server requests per time period; Duration and rate of database calls per time period;
        /// Number of sale events and number of items sold per sale event over a time period, etc.</para>
        /// </summary>
        /// <param name="metricConfigPresets"></param>
        /// <returns></returns>
        public static MetricConfiguration Measurement(this MetricConfigurations metricConfigPresets)
        {
            return s_defaultConfigForMeasurement;
        }

        /// <summary>
        /// Set the configuration returnred from <c>MetricConfigurations.Use.Measurement()</c>.
        /// </summary>
        /// <param name="metricConfigPresets">Will be ignored.</param>
        /// <param name="defaultConfigurationForMeasurement">Future default config.</param>
        public static void SetDefaultForMeasurement(
                                                this MetricConfigurations metricConfigPresets,
                                                MetricConfiguration defaultConfigurationForMeasurement)
        {
            Util.ValidateNotNull(defaultConfigurationForMeasurement, nameof(defaultConfigurationForMeasurement));
            Util.ValidateNotNull(defaultConfigurationForMeasurement.SeriesConfig, nameof(defaultConfigurationForMeasurement) + "." + nameof(defaultConfigurationForMeasurement.SeriesConfig));
            
            if (false == (defaultConfigurationForMeasurement.SeriesConfig is MetricSeriesConfigurationForMeasurement))
            {
                throw new ArgumentException($"{nameof(defaultConfigurationForMeasurement) + "." + nameof(defaultConfigurationForMeasurement.SeriesConfig)}"
                                          + $" must be a \"{nameof(MetricSeriesConfigurationForMeasurement)}\", but it is"
                                          + $" \"{defaultConfigurationForMeasurement.SeriesConfig.GetType().Name}\".");
            }

            s_defaultConfigForMeasurement = defaultConfigurationForMeasurement;
        }

        internal static MetricConfiguration Default(this MetricConfigurations metricConfigPresets)
        {
            return metricConfigPresets.Measurement();
        }
    }
}
