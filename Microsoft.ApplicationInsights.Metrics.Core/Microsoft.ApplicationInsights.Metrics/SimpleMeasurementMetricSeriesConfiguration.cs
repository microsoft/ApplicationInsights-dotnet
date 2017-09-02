using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleMeasurementMetricSeriesConfiguration : IMetricSeriesConfiguration
    {
        private readonly bool _lifetimeCounter;
        private readonly bool _supportDoubleValues;
        private readonly int _hashCode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lifetimeCounter"></param>
        /// <param name="supportDoubleValues"></param>
        public SimpleMeasurementMetricSeriesConfiguration(bool lifetimeCounter, bool supportDoubleValues)
        {
            _lifetimeCounter = lifetimeCounter;
            _supportDoubleValues = supportDoubleValues;

            unchecked
            {
                _hashCode = (((17 * 23) + _lifetimeCounter.GetHashCode()) * 23) + _supportDoubleValues.GetHashCode();
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
        public bool SupportDoubleValues
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _supportDoubleValues; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="consumerKind"></param>
        /// <returns></returns>
        public IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricConsumerKind consumerKind)
        {
            if (_supportDoubleValues)
            {
                IMetricSeriesAggregator aggregator = new SimpleDoubleDataSeriesAggregator(this, dataSeries, consumerKind);
                return aggregator;
            }
            else
            {
                IMetricSeriesAggregator aggregator = new SimpleUIntDataSeriesAggregator(this, dataSeries, consumerKind);
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
                var otherConfig = obj as SimpleMeasurementMetricSeriesConfiguration;
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
        public bool Equals(SimpleMeasurementMetricSeriesConfiguration other)
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
                || (this.SupportDoubleValues == other.SupportDoubleValues);
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
