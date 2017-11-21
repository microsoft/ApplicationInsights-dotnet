using System;
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
                                                                    MetricConfigurations.Common.AggregateKinds().Accumulator().Moniker,
                                                                    new AccumulatorAggregateToApplicationInsightsPipelineConverter());
        }

        /// <summary />
        /// <param name="restrictToUInt32Values"></param>
        public MetricSeriesConfigurationForAccumulator(bool restrictToUInt32Values)
        {
            _restrictToUInt32Values = restrictToUInt32Values;

            _hashCode = Util.CombineHashCodes(1, _restrictToUInt32Values.GetHashCode());
        }

        /// <summary />
        public bool AutoCleanupUnusedSeries
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return false; }
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
    }
}
