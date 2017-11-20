using System;

using Microsoft.ApplicationInsights.Metrics;

using Util = Microsoft.ApplicationInsights.Metrics.Extensions.Util;
using System.ComponentModel;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// <para>Provides the default metric configurations defines in the Metrics Extensions package.</para>
    /// <para>Do not use directly. Instead, use: <c>MetricConfigurations.Common.Xxxx()</c>. </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetricConfigurationsExtensions
    {
        private const int DefaultSeriesCountLimit = 1000;
        private const int DefaultaluesPerDimensionLimit = 100;

        private static IMetricConfiguration s_defaultConfigForAccumulator = new SimpleMetricConfiguration(
                                                        DefaultSeriesCountLimit,
                                                        DefaultaluesPerDimensionLimit,
                                                        new MetricSeriesConfigurationForAccumulator(restrictToUInt32Values: false));

        /// <summary>
        /// <para>Use for measuring and accumulating differences between states of an entity that exists over a long period of time.<br />
        /// Will keep the accumulated state and will not automatically reset at the end of each time period.<br />
        /// Produces aggregates that contain accumulated statistics about tracked Deltas over the entire life-time of the
        /// metric in memory (or since an explicit reset): Sum, Min, Max.</para>
        /// 
        /// <para>For example, use this metric configuration to measure:<br />
        /// Number of service invocations in-flight (.TrackValue(1) / .TrackValue(-1) when invocations begin/end);<br />
        /// Count of items in a memory data structure (.TrackValue(n) / .TrackValue(-m) when items are added / removed);<br />
        /// Volume of water in a container (.TrackValue(litersIn) / .TrackValue(-litersOut) when water flows in or out).</para>
        /// </summary>
        public static IMetricConfiguration Accumulator(this MetricConfigurations metricConfigPresets)
        {
            return s_defaultConfigForAccumulator;
        }

        /// <summary>
        /// Set the configuration returnred from <c>MetricConfigurations.Use.Accumulator()</c>.
        /// </summary>
        /// <param name="metricConfigPresets">Will be ignored.</param>
        /// <param name="defaultConfigurationForAccumulator">Future default config.</param>
        public static void SetDefaultForAccumulator(
                                                this MetricConfigurations metricConfigPresets,
                                                SimpleMetricConfiguration defaultConfigurationForAccumulator)
        {
            Util.ValidateNotNull(defaultConfigurationForAccumulator, nameof(defaultConfigurationForAccumulator));

            // todo validate type of series config to be measurement.

            s_defaultConfigForAccumulator = defaultConfigurationForAccumulator;
        }

    }
}
