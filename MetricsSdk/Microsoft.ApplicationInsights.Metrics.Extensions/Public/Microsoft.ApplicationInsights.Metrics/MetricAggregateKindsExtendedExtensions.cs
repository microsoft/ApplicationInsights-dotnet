using System;
using System.ComponentModel;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// Provides discoverable access to constants used by metric aggregates.
    /// Do not use directly. Instead, use: <c>MetricConfigurations.Common.AggregateKinds()</c>.
    /// See also: <see cref="MetricAggregateKinds" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetricAggregateKindsExtendedExtensions
    {
        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForGauge"/>. See also <c>MetricConfigurations.Common.Gauge()</c>./>
        /// </summary>
        /// <param name="aggregateKinds"></param>
        /// <returns></returns>
        public static MetricAggregateKindsExtendedExtensions.MetricAggregateKinds.Gauge Gauge(
                                                                        this Microsoft.ApplicationInsights.Metrics.MetricAggregateKinds aggregateKinds)
        {
            return MetricAggregateKinds.Gauge.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>. See also <c>MetricConfigurations.Common.Accumulator()</c>./>
        /// </summary>
        /// <param name="aggregateKinds"></param>
        /// <returns></returns>
        public static MetricAggregateKindsExtendedExtensions.MetricAggregateKinds.Accumulator Accumulator(
                                                                        this Microsoft.ApplicationInsights.Metrics.MetricAggregateKinds aggregateKinds)
        {
            return MetricAggregateKinds.Accumulator.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForNaiveDistinctCount"/>. See also <c>MetricConfigurations.Common.NaiveDistinctCount()</c>./>
        /// </summary>
        /// <param name="aggregateKinds"></param>
        /// <returns></returns>
        public static MetricAggregateKindsExtendedExtensions.MetricAggregateKinds.NaiveDistinctCount NaiveDistinctCount(
                                                                        this Microsoft.ApplicationInsights.Metrics.MetricAggregateKinds aggregateKinds)
        {
            return MetricAggregateKinds.NaiveDistinctCount.Instance;
        }

        /// <summary />
        public static class MetricAggregateKinds
        {
            /// <summary>
            /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForGauge"/>. See also <c>MetricConfigurations.Common.Gauge()</c>./>
            /// </summary>
            public sealed class Gauge
            {
                internal static readonly Gauge Instance = new Gauge();

                private Gauge()
                {
                }

                /// <summary />
                public string Moniker { get { return Constants.Gauge.Moniker; } }

                /// <summary />
                public DataKeysConstants DataKeys { get { return DataKeysConstants.Instance; } }

                /// <summary />
                public sealed class DataKeysConstants
                {
                    internal static readonly DataKeysConstants Instance = new DataKeysConstants();

                    private DataKeysConstants()
                    {
                    }

                    /// <summary />
                    public string Last { get { return Constants.Gauge.DataKeys.Last; } }

                    /// <summary />
                    public string Min { get { return Constants.Gauge.DataKeys.Min; } }

                    /// <summary />
                    public string Max { get { return Constants.Gauge.DataKeys.Max; } }
                }
            }


            /// <summary>
            /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>. See also <c>MetricConfigurations.Common.Accumulator()</c>./>
            /// </summary>
            public sealed class Accumulator
            {
                internal static readonly Accumulator Instance = new Accumulator();

                private Accumulator()
                {
                }

                /// <summary />
                public string Moniker { get { return Constants.Accumulator.Moniker; } }

                /// <summary />
                public DataKeysConstants DataKeys { get { return DataKeysConstants.Instance; } }

                /// <summary />
                public sealed class DataKeysConstants
                {
                    internal static readonly DataKeysConstants Instance = new DataKeysConstants();

                    private DataKeysConstants()
                    {
                    }

                    /// <summary />
                    public string Sum { get { return Constants.Accumulator.DataKeys.Sum; } }

                    /// <summary />
                    public string Min { get { return Constants.Accumulator.DataKeys.Min; } }

                    /// <summary />
                    public string Max { get { return Constants.Accumulator.DataKeys.Max; } }
                }
            }

            /// <summary>
            /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForNaiveDistinctCount"/>. See also <c>MetricConfigurations.Common.NaiveDistinctCount()</c>./>
            /// </summary>
            public sealed class NaiveDistinctCount
            {
                internal static readonly NaiveDistinctCount Instance = new NaiveDistinctCount();

                private NaiveDistinctCount()
                {
                }

                /// <summary />
                public string Moniker { get { return Constants.NaiveDistinctCount.Moniker; } }

                /// <summary />
                public DataKeysConstants DataKeys { get { return DataKeysConstants.Instance; } }

                /// <summary />
                public sealed class DataKeysConstants
                {
                    internal static readonly DataKeysConstants Instance = new DataKeysConstants();

                    private DataKeysConstants()
                    {
                    }

                    /// <summary />
                    public string TotalCount { get { return Constants.NaiveDistinctCount.DataKeys.TotalCount; } }

                    /// <summary />
                    public string DistinctCount { get { return Constants.NaiveDistinctCount.DataKeys.DistinctCount; } }
                }
            }


            /// <summary />
            private static class Constants
            {
                /// <summary />
                public static class Gauge
                {
                    /// <summary />
                    public const string Moniker = "Microsoft.Azure.Gauge";

                    /// <summary />
                    public static class DataKeys
                    {
                        /// <summary />
                        public const string Last = "Last";

                        /// <summary />
                        public const string Min = "Min";

                        /// <summary />
                        public const string Max = "Max";
                    }
                }

                /// <summary />
                public static class Accumulator
                {
                    /// <summary />
                    public const string Moniker = "Microsoft.Azure.Accumulator";

                    /// <summary />
                    public static class DataKeys
                    {
                        /// <summary />
                        public const string Sum = "Sum";

                        /// <summary />
                        public const string Min = "Min";

                        /// <summary />
                        public const string Max = "Max";
                    }
                }

                /// <summary />
                public static class NaiveDistinctCount
                {
                    /// <summary />
                    public const string Moniker = "Microsoft.Azure.NaiveDistinctCount";

                    /// <summary />
                    public static class DataKeys
                    {
                        /// <summary />
                        public const string TotalCount = "TotalCount";

                        /// <summary />
                        public const string DistinctCount = "DistinctCount";
                    }
                }
            }
        }
    }
}
