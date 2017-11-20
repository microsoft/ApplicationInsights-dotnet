using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    public class MetricSeriesConfigurationForMeasurement : IMetricSeriesConfiguration
    {
        static MetricSeriesConfigurationForMeasurement()
        {
            MetricAggregateToTelemetryPipelineConverters.Registry.Add(
                                                                    typeof(ApplicationInsightsTelemetryPipeline),
                                                                    MetricConfigurations.Common.AggregateKinds().Measurement().Moniker,
                                                                    new MeasurementAggregateToApplicationInsightsPipelineConverter());
        }

        private readonly bool _restrictToUInt32Values;
        private readonly int _hashCode;

        /// <summary />
        /// <param name="restrictToUInt32Values"></param>
        public MetricSeriesConfigurationForMeasurement(bool restrictToUInt32Values)
        {
            _restrictToUInt32Values = restrictToUInt32Values;

            unchecked
            {
                _hashCode = (((17 * 23) + _restrictToUInt32Values.GetHashCode()) * 23);
            }
        }

        /// <summary />
        public bool RequiresPersistentAggregation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return false; }
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
            IMetricSeriesAggregator aggregator = new MeasurementAggregator(this, dataSeries, aggregationCycleKind);
            return aggregator;
        }

        /// <summary />
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricSeriesConfigurationForMeasurement;
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
        public bool Equals(MetricSeriesConfigurationForMeasurement other)
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
