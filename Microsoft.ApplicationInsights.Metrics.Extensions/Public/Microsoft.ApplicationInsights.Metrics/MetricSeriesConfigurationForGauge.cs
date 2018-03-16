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
    public class MetricSeriesConfigurationForGauge : IMetricSeriesConfiguration
    {
        private readonly bool _alwaysResendLastValue;
        private readonly bool _restrictToUInt32Values;
        private readonly int _hashCode;

        static MetricSeriesConfigurationForGauge()
        {
            MetricAggregateToTelemetryPipelineConverters.Registry.Add(
                                                                    typeof(ApplicationInsightsTelemetryPipeline),
                                                                    MetricSeriesConfigurationForGauge.Constants.AggregateKindMoniker,
                                                                    new GaugeAggregateToApplicationInsightsPipelineConverter());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alwaysResendLastValue"></param>
        /// <param name="restrictToUInt32Values"></param>
        public MetricSeriesConfigurationForGauge(bool alwaysResendLastValue, bool restrictToUInt32Values)
        {
            _alwaysResendLastValue = alwaysResendLastValue;
            _restrictToUInt32Values = restrictToUInt32Values;

            _hashCode = Util.CombineHashCodes(_alwaysResendLastValue.GetHashCode(), _restrictToUInt32Values.GetHashCode());
        }

        /// <summary />
        public bool RequiresPersistentAggregation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _alwaysResendLastValue; }
        }

        /// <summary />
        public bool AlwaysResendLastValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _alwaysResendLastValue; }
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
            IMetricSeriesAggregator aggregator = new GaugeAggregator(this, dataSeries, aggregationCycleKind);
            return aggregator;
        }

        /// <summary />
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricSeriesConfigurationForGauge;
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
        public bool Equals(MetricSeriesConfigurationForGauge other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return (this.AlwaysResendLastValue == other.AlwaysResendLastValue)
                && (this.RestrictToUInt32Values == other.RestrictToUInt32Values);
        }

        /// <summary />
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Groups constants used by metric aggregates produced by aggregators that are configured by metric configurations represented through
        /// instances of <see cref="MetricSeriesConfigurationForGauge"/>. This class cannot be instantiated. To access the constants, use the 
        /// extension method <c>MetricConfigurations.Common.Gauge().Constants()</c> or <see cref="MetricSeriesConfigurationForGauge.Constants"/>.
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
            /// through instances of <see cref="MetricSeriesConfigurationForGauge"/>.
            /// </summary>
            public string AggregateKindMoniker { get { return Constants.AggregateKindMoniker; } }

            /// <summary>
            /// Constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForGauge"/>.
            /// </summary>
            public DataKeysConstants AggregateKindDataKeys { get { return DataKeysConstants.Instance; } }

            /// <summary>
            /// Groups constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForGauge"/>.
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class DataKeysConstants
            {
                internal static readonly DataKeysConstants Instance = new DataKeysConstants();

                private DataKeysConstants()
                {
                }

                /// <summary>
                /// The name of the Last field in <see cref="MetricAggregate"/> objects produced by gauge aggregators.
                /// </summary>
                public string Last { get { return Constants.AggregateKindDataKeys.Last; } }

                /// <summary>
                /// The name of the Minimum field in <see cref="MetricAggregate"/> objects produced by gauge aggregators.
                /// </summary>
                public string Min { get { return Constants.AggregateKindDataKeys.Min; } }

                /// <summary>
                /// The name of the Maximum field in <see cref="MetricAggregate"/> objects produced by gauge aggregators.
                /// </summary>
                public string Max { get { return Constants.AggregateKindDataKeys.Max; } }
            }
        }

        /// <summary>
        /// Defines constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForGauge"/>.
        /// </summary>
        public static class Constants
        {
            /// <summary>
            /// The kind moniker for aggregates produced by aggregators that are configured by metric configurations represented
            /// through instances of <see cref="MetricSeriesConfigurationForGauge"/>.
            /// </summary>
            public const string AggregateKindMoniker = "Microsoft.Azure.Gauge";

            /// <summary>
            /// Defines constants used to refer to data fields contained within aggregates produced by aggregators that are configured
            /// by metric configurations represented through instances of <see cref="MetricSeriesConfigurationForGauge"/>.
            /// </summary>
            public static class AggregateKindDataKeys
            {
                /// <summary>
                /// The name of the Last field in <see cref="MetricAggregate"/> objects produced by gauge aggregators.
                /// </summary>
                public const string Last = "Last";

                /// <summary>
                /// The name of the Minimum field in <see cref="MetricAggregate"/> objects produced by gauge aggregators.
                /// </summary>
                public const string Min = "Min";

                /// <summary>
                /// The name of the Maximum field in <see cref="MetricAggregate"/> objects produced by gauge aggregators.
                /// </summary>
                public const string Max = "Max";
            }
        }
    }
}
