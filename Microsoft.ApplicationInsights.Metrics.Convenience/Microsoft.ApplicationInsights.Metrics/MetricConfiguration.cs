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
        private static IMetricConfiguration s_measurementInt32;
        private static IMetricConfiguration s_measurementDouble;
        private static IMetricConfiguration s_counterUInt32;
        private static IMetricConfiguration s_counterDouble;

        static MetricConfiguration()
        {
            ReInitialize();
        }

        /// <summary>
        /// 
        /// </summary>
        internal static IMetricConfiguration Default { get { return MeasurementDouble; } }

        /// <summary>
        /// 
        /// </summary>
        public static IMetricConfiguration MeasurementUInt32 { get { return s_measurementInt32; } }

        /// <summary>
        /// 
        /// </summary>
        public static IMetricConfiguration MeasurementDouble { get { return s_measurementDouble; } }

        /// <summary>
        /// 
        /// </summary>
        public static IMetricConfiguration CounterUInt32 { get { return s_counterUInt32; } }

        /// <summary>
        /// 
        /// </summary>
        public static IMetricConfiguration CounterDouble { get { return s_counterDouble; } }


        private static void ReInitialize()
        {
            s_measurementInt32 = new SimpleMeasurementMetricConfiguration(
                                                        Defaults.SeriesCountLimit,
                                                        Defaults.ValuesPerDimensionLimit,
                                                        Defaults.NewSeriesCreationTimeout,
                                                        Defaults.NewSeriesCreationRetryDelay,
                                                        new SimpleMetricSeriesConfiguration(
                                                                        lifetimeCounter: false,
                                                                        restrictToUInt32Values: true));

            s_measurementDouble = new SimpleMeasurementMetricConfiguration(
                                                        Defaults.SeriesCountLimit,
                                                        Defaults.ValuesPerDimensionLimit,
                                                        Defaults.NewSeriesCreationTimeout,
                                                        Defaults.NewSeriesCreationRetryDelay,
                                                        new SimpleMetricSeriesConfiguration(
                                                                        lifetimeCounter: false,
                                                                        restrictToUInt32Values: false));

            s_counterUInt32 = new SimpleMeasurementMetricConfiguration(
                                                        Defaults.SeriesCountLimit,
                                                        Defaults.ValuesPerDimensionLimit,
                                                        Defaults.NewSeriesCreationTimeout,
                                                        Defaults.NewSeriesCreationRetryDelay,
                                                        new SimpleMetricSeriesConfiguration(
                                                                        lifetimeCounter: true,
                                                                        restrictToUInt32Values: true));

            s_counterDouble = new SimpleMeasurementMetricConfiguration(
                                                        Defaults.SeriesCountLimit,
                                                        Defaults.ValuesPerDimensionLimit,
                                                        Defaults.NewSeriesCreationTimeout,
                                                        Defaults.NewSeriesCreationRetryDelay,
                                                        new SimpleMetricSeriesConfiguration(
                                                                        lifetimeCounter: true,
                                                                        restrictToUInt32Values: false));
        }


        #region class Defaults

        /// <summary>
        /// 
        /// </summary>
        public static class Defaults
        {
            internal static readonly TimeSpan NewSeriesCreationTimeout = TimeSpan.FromMilliseconds(10);
            internal static readonly TimeSpan NewSeriesCreationRetryDelay = TimeSpan.FromMilliseconds(1);

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
                    if (value < 2)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(SeriesCountLimit)} may not be < 2.");
                    }

                    Interlocked.Exchange(ref s_valuesPerDimensionLimit, value);    // benign race;
                    MetricConfiguration.ReInitialize();
                }
            }
        }

        #endregion class Defaults
    }
}
