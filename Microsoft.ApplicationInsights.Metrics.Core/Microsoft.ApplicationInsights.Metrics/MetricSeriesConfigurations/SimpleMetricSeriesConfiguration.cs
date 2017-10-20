using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleMetricSeriesConfiguration : IMetricSeriesConfiguration
    {
        private readonly bool _lifetimeCounter;
        private readonly bool _restrictToUInt32Values;
        private readonly int _hashCode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lifetimeCounter"></param>
        /// <param name="restrictToUInt32Values"></param>
        public SimpleMetricSeriesConfiguration(bool lifetimeCounter, bool restrictToUInt32Values)
        {
            _lifetimeCounter = lifetimeCounter;
            _restrictToUInt32Values = restrictToUInt32Values;

            unchecked
            {
                _hashCode = (((17 * 23) + _lifetimeCounter.GetHashCode()) * 23) + _restrictToUInt32Values.GetHashCode();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RequiresPersistentAggregation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _lifetimeCounter; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RestrictToUInt32Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _restrictToUInt32Values; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <returns></returns>
        public IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
        {
            if (_restrictToUInt32Values)
            {
                IMetricSeriesAggregator aggregator = new SimpleUInt32DataSeriesAggregator(this, dataSeries, aggregationCycleKind);
                return aggregator;
            }
            else
            {
                IMetricSeriesAggregator aggregator = new SimpleDoubleDataSeriesAggregator(this, dataSeries, aggregationCycleKind);
                return aggregator;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as SimpleMetricSeriesConfiguration;
                if (otherConfig != null)
                {
                    return Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IMetricSeriesConfiguration other)
        {
            return Equals((object) other);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SimpleMetricSeriesConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return (this.RequiresPersistentAggregation == other.RequiresPersistentAggregation)
                && (this.RestrictToUInt32Values == other.RestrictToUInt32Values);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
