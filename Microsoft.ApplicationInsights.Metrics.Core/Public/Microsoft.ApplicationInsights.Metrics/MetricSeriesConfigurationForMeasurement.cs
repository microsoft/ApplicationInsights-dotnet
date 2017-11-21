using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    public class MetricSeriesConfigurationForMeasurement : IMetricSeriesConfiguration
    {
        private readonly bool _restrictToUInt32Values;
        private readonly bool _autoCleanupUnusedSeries;
        private readonly int _hashCode;

        static MetricSeriesConfigurationForMeasurement()
        {
            MetricAggregateToTelemetryPipelineConverters.Registry.Add(
                                                                    typeof(ApplicationInsightsTelemetryPipeline),
                                                                    MetricConfigurations.Common.AggregateKinds().Measurement().Moniker,
                                                                    new MeasurementAggregateToApplicationInsightsPipelineConverter());
        }

        /// <summary />
        /// <param name="autoCleanupUnusedSeries"></param>
        /// <param name="restrictToUInt32Values"></param>
        public MetricSeriesConfigurationForMeasurement(bool autoCleanupUnusedSeries, bool restrictToUInt32Values)
        {
            _autoCleanupUnusedSeries = autoCleanupUnusedSeries;
            _restrictToUInt32Values = restrictToUInt32Values;

            _hashCode = Util.CombineHashCodes(
                                           _autoCleanupUnusedSeries.GetHashCode(),
                                           _restrictToUInt32Values.GetHashCode());
        }

        /// <summary />
        public bool AutoCleanupUnusedSeries
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _autoCleanupUnusedSeries; }
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
