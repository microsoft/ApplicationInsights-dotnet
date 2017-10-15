using System;
using System.Threading;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public static class MetricConfiguration
    {
        private static IMetricConfiguration s_measurementDouble;
        private static IMetricConfiguration s_counterDouble;

        static MetricConfiguration()
        {
            ReInitialize();
        }

        /// <summary>
        /// </summary>
        internal static IMetricConfiguration Default { get { return Measurement; } }


        /// <summary>
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
                    MetricConfiguration.ReInitialize();
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
                    MetricConfiguration.ReInitialize();
                }
            }
        }

        #endregion class Defaults
    }
}
