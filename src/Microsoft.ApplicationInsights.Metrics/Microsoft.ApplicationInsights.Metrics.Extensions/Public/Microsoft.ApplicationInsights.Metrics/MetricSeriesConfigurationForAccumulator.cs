using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensions;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public class MetricSeriesConfigurationForAccumulator : IMetricSeriesConfiguration
    {
        private readonly bool _restrictToUInt32Values;
        private readonly int _hashCode;

        static MetricSeriesConfigurationForAccumulator()
        {
            MetricAggregateToTelemetryPipelineConverters.Registry.Add(
                                                                    typeof(ApplicationInsightsTelemetryPipeline),
                                                                    MetricSeriesConfigurationForAccumulator.Constants.AggregateKindMoniker,
                                                                    new AccumulatorAggregateToApplicationInsightsPipelineConverter());
        }

        /// <summary />
        /// <param name="restrictToUInt32Values"></param>
        public MetricSeriesConfigurationForAccumulator(bool restrictToUInt32Values)
        {
            _restrictToUInt32Values = restrictToUInt32Values;

            _hashCode = Util.CombineHashCodes(_restrictToUInt32Values.GetHashCode());
        }

        /// <summary />
        public bool RequiresPersistentAggregation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return true; }
        }

        /// <summary />
        public bool RestrictToUInt32Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _restrictToUInt32Values; }
        }

        /// <summary />
        /// <param name="dataSeries"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <returns></returns>
        public IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
        {
            IMetricSeriesAggregator aggregator = new AccumulatorAggregator(this, dataSeries, aggregationCycleKind);
            return aggregator;
        }

        /// <summary />
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricSeriesConfigurationForAccumulator;
                if (otherConfig != null)
                {
                    return Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary />
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IMetricSeriesConfiguration other)
        {
            return Equals((object) other);
        }

        /// <summary />
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(MetricSeriesConfigurationForAccumulator other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return (this.RestrictToUInt32Values == other.RestrictToUInt32Values);
        }

        /// <summary />
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Groups constants used by metric aggregates produced by aggregators that are configured by metric configurations represented through
        /// instances of <see cref="MetricSeriesConfigurationForAccumulator"/>. This class cannot be instantiated. To access the constants, use the 
        /// extension method <c>MetricConfigurations.Common.Accumulator().Constants()</c> or <see cref="MetricSeriesConfigurationForAccumulator.Constants"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class AggregateKindConstants
        {
            internal static readonly AggregateKindConstants Instance = new AggregateKindConstants();

            private AggregateKindConstants()
            {
            }

            /// <summary>
            /// The kind moniker for aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>.
            /// </summary>
            public string AggregateKindMoniker { get { return Constants.AggregateKindMoniker; } }

            /// <summary>
            /// Constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>.
            /// </summary>
            public DataKeysConstants AggregateKindDataKeys { get { return DataKeysConstants.Instance; } }

            /// <summary>
            /// Groups constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>.
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class DataKeysConstants
            {
                internal static readonly DataKeysConstants Instance = new DataKeysConstants();

                private DataKeysConstants()
                {
                }

                /// <summary>
                /// The name of the Sum field in <see cref="MetricAggregate"/> objects produced by accumulator aggregators.
                /// </summary>
                public string Sum { get { return Constants.AggregateKindDataKeys.Sum; } }

                /// <summary>
                /// The name of the Minimum field in <see cref="MetricAggregate"/> objects produced by accumulator aggregators.
                /// </summary>
                public string Min { get { return Constants.AggregateKindDataKeys.Min; } }

                /// <summary>
                /// The name of the Maximum field in <see cref="MetricAggregate"/> objects produced by accumulator aggregators.
                /// </summary>
                public string Max { get { return Constants.AggregateKindDataKeys.Max; } }
            }
        }

        /// <summary>
        /// Defines constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>.
        /// </summary>
        public static class Constants
        {
            /// <summary>
            /// The kind moniker for aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>.
            /// </summary>
            public const string AggregateKindMoniker = "Microsoft.Azure.Accumulator";

            /// <summary>
            /// Defines constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>.
            /// </summary>
            public static class AggregateKindDataKeys
            {
                /// <summary>
                /// The name of the Sum field in <see cref="MetricAggregate"/> objects produced by accumulator aggregators.
                /// </summary>
                public const string Sum = "Sum";

                /// <summary>
                /// The name of the Minimum field in <see cref="MetricAggregate"/> objects produced by accumulator aggregators.
                /// </summary>
                public const string Min = "Min";

                /// <summary>
                /// The name of the Maximum field in <see cref="MetricAggregate"/> objects produced by accumulator aggregators.
                /// </summary>
                public const string Max = "Max";
            }
        }
    }
}
