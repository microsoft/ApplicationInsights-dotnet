using System;
using System.Threading;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Static container for the most commonly used metric configurations.
    /// </summary>
    public static class MetricConfigurations
    {
        private static IMetricConfiguration s_measurementDouble;
        private static IMetricConfiguration s_accumulatorDouble;

        static MetricConfigurations()
        {
            ReInitialize();
        }

        internal static IMetricConfiguration Default { get { return Measurement; } }


        /// <summary>
        /// <para>Use to measure attributes and/or counts of items. Also, use to measure attributes and/or rates of events.<br />
        /// Produces aggregates that contain simple statistics about tracked values per time period: Count, Sum, Min, Max.<br />
        /// (This is the most commonly used metric configuration and is the default unless otherwise specified.)</para>
        /// 
        /// <para>For example, use this metric configuration to measure:<br />
        /// Size and number of server requests per time period; Duration and rate of database calls per time period;
        /// Number of sale events and number of items sold per sale event over a time period, etc.</para>
        /// </summary>
        public static IMetricConfiguration Measurement { get { return s_measurementDouble; } }

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
        public static IMetricConfiguration Accumulator { get { return s_accumulatorDouble; } }


        private static void ReInitialize()
        {
            s_measurementDouble = new SimpleMetricConfiguration(
                                                        FutureDefaults.SeriesCountLimit,
                                                        FutureDefaults.ValuesPerDimensionLimit,
                                                        new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            s_accumulatorDouble = new SimpleMetricConfiguration(
                                                        FutureDefaults.SeriesCountLimit,
                                                        FutureDefaults.ValuesPerDimensionLimit,
                                                        new MetricSeriesConfigurationForAccumulator(restrictToUInt32Values: false));
        }


        #region class Defaults

        /// <summary>
        /// Used to change the default attributes of the static members of <see cref="MetricConfigurations" />.
        /// </summary>
        public static class FutureDefaults
        {
            private static int s_seriesCountLimit = 1000;
            private static int s_valuesPerDimensionLimit = 100;

            /// <summary>
            /// The max number of time series per metric for metrics that use <see cref="MetricConfigurations.Measurement" />
            /// or <see cref="MetricConfigurations.Accumulator" /> configurations.
            /// </summary>
            public static int SeriesCountLimit
            {
                get
                {
                    return s_seriesCountLimit;
                }

                set
                {
                    if (value < 2)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(SeriesCountLimit)} may not be < 2.");
                    }

                    Interlocked.Exchange(ref s_seriesCountLimit, value);    // benign race;
                    MetricConfigurations.ReInitialize();
                }
            }

            /// <summary>
            /// The max number of distinct values per dimension for metrics that use <see cref="MetricConfigurations.Measurement" />
            /// or <see cref="MetricConfigurations.Accumulator" /> configurations.
            /// </summary>
            public static int ValuesPerDimensionLimit
            {
                get
                {
                    return s_valuesPerDimensionLimit;
                }

                set
                {
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(ValuesPerDimensionLimit)} may not be < 1.");
                    }

                    Interlocked.Exchange(ref s_valuesPerDimensionLimit, value);    // benign race;
                    MetricConfigurations.ReInitialize();
                }
            }
        }

        #endregion class Defaults
    }
}
