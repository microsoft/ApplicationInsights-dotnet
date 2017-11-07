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
        private static IMetricConfiguration s_counterDouble;

        static MetricConfigurations()
        {
            ReInitialize();
        }

        internal static IMetricConfiguration Default { get { return Measurement; } }


        /// <summary>
        /// <para>Use to measure attributes and/or counts of items. Also, use to measure attributes and/or rates of events.<br />
        /// Will produce aggregates that contain simple statistics about tracked values per time period: Count, Sum, Min, Max.<br />
        /// (This is the most commonly used metric configuration and is the default unless otherwise specified.)</para>
        /// 
        /// <para>For example, use <c>MetricConfigurations.Measurement</c> to measure:<br />
        /// Size and number of server requests per time period, Duration and rate of database calls per time period,
        /// Number of sale events and number of items sold per sale event over a time period, etc.    
        /// </para>
        /// </summary>
        public static IMetricConfiguration Measurement { get { return s_measurementDouble; } }

        /// <summary>
        /// 
        /// </summary>
        public static IMetricConfiguration Counter { get { return s_counterDouble; } }


        private static void ReInitialize()
        {
            s_measurementDouble = new SimpleMetricConfiguration(
                                                        FutureDefaults.SeriesCountLimit,
                                                        FutureDefaults.ValuesPerDimensionLimit,
                                                        //FutureDefaults.NewSeriesCreationRetryDelay,
                                                        //FutureDefaults.NewSeriesCreationTimeout,
                                                        new SimpleMetricSeriesConfiguration(
                                                                        lifetimeCounter: false,
                                                                        restrictToUInt32Values: false));

            s_counterDouble = new SimpleMetricConfiguration(
                                                        FutureDefaults.SeriesCountLimit,
                                                        FutureDefaults.ValuesPerDimensionLimit,
                                                        //FutureDefaults.NewSeriesCreationRetryDelay,
                                                        //FutureDefaults.NewSeriesCreationTimeout,
                                                        new SimpleMetricSeriesConfiguration(
                                                                        lifetimeCounter: true,
                                                                        restrictToUInt32Values: false));
        }


        #region class Defaults

        /// <summary>
        /// 
        /// </summary>
        public static class FutureDefaults
        {
            //internal static readonly TimeSpan NewSeriesCreationRetryDelay = TimeSpan.FromMilliseconds(1);
            //internal static readonly TimeSpan NewSeriesCreationTimeout = TimeSpan.FromMilliseconds(10);

            private static int s_seriesCountLimit = 1000;
            private static int s_valuesPerDimensionLimit = 100;

            /// <summary>
            /// 
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
            /// 
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
