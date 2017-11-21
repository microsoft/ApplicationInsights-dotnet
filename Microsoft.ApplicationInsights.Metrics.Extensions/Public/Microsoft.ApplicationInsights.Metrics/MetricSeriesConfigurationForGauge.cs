using System;
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
        private readonly bool _autoCleanupUnusedSeries;
        private readonly bool _restrictToUInt32Values;
        private readonly int _hashCode;

        static MetricSeriesConfigurationForGauge()
        {
            MetricAggregateToTelemetryPipelineConverters.Registry.Add(
                                                                    typeof(ApplicationInsightsTelemetryPipeline),
                                                                    MetricConfigurations.Common.AggregateKinds().Gauge().Moniker,
                                                                    new GaugeAggregateToApplicationInsightsPipelineConverter());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alwaysResendLastValue"></param>
        /// <param name="autoCleanupUnusedSeries"></param>
        /// <param name="restrictToUInt32Values"></param>
        public MetricSeriesConfigurationForGauge(bool alwaysResendLastValue, bool autoCleanupUnusedSeries, bool restrictToUInt32Values)
        {
            if (alwaysResendLastValue && autoCleanupUnusedSeries)
            {
                throw new ArgumentException($"{nameof(autoCleanupUnusedSeries)} may not be True if {nameof(alwaysResendLastValue)} is True,"
                                          + $" becasue {nameof(alwaysResendLastValue)} requires persistent aggregation which"
                                          + $" prevents {nameof(autoCleanupUnusedSeries)} from taking effect."
                                           + " Set at lease one of these two options to False.");
            }

            _alwaysResendLastValue = alwaysResendLastValue;
            _autoCleanupUnusedSeries = autoCleanupUnusedSeries;
            _restrictToUInt32Values = restrictToUInt32Values;

            _hashCode = Util.CombineHashCodes(
                                            _autoCleanupUnusedSeries.GetHashCode(),
                                            _alwaysResendLastValue.GetHashCode(),
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
    }
}
