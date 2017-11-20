using System;
using System.ComponentModel;

using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// <para>Provides the default Metric Configuration for Measurements.</para>
    /// <para>Do not use directly. Instead, use: <c>MetricConfigurations.Common.Xxxx()</c>. </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetricConfigurationsExtensions
    {
        private const int DefaultSeriesCountLimit = 1000;
        private const int DefaultaluesPerDimensionLimit = 100;

        private static SimpleMetricConfiguration s_defaultConfigForMeasurement = new SimpleMetricConfiguration(
                                                        DefaultSeriesCountLimit,
                                                        DefaultaluesPerDimensionLimit,
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
        public static IMetricConfiguration Measurement(this MetricConfigurations metricConfigPresets)
        {
            return s_defaultConfigForMeasurement;
        }

        internal static IMetricConfiguration Default(this MetricConfigurations metricConfigPresets)
        {
            return metricConfigPresets.Measurement();
        }

        /// <summary>
        /// Set the configuration returnred from <c>MetricConfigurations.Use.Measurement()</c>.
        /// </summary>
        /// <param name="metricConfigPresets">Will be ignored.</param>
        /// <param name="defaultConfigurationForMeasurement">Future default config.</param>
        public static void SetDefaultForMeasurement(
                                                this MetricConfigurations metricConfigPresets,
                                                SimpleMetricConfiguration defaultConfigurationForMeasurement)
        {
            Util.ValidateNotNull(defaultConfigurationForMeasurement, nameof(defaultConfigurationForMeasurement));

            // todo validate type of series config to be measurement.

            s_defaultConfigForMeasurement = defaultConfigurationForMeasurement;
        }
    }
}
