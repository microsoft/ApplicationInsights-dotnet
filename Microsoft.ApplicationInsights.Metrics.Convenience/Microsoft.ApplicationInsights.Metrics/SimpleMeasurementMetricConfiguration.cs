using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleMeasurementMetricConfiguration : IMetricConfiguration
    {
        private readonly int _hashCode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="seriesConfig"></param>
        public SimpleMeasurementMetricConfiguration(int seriesCountLimit, int valuesPerDimensionLimit, IMetricSeriesConfiguration seriesConfig)
            : this(
                    seriesCountLimit,
                    valuesPerDimensionLimit,
                    MetricConfiguration.Defaults.NewSeriesCreationTimeout,
                    MetricConfiguration.Defaults.NewSeriesCreationRetryDelay,
                    seriesConfig)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="newSeriesCreationTimeout"></param>
        /// <param name="newSeriesCreationRetryDelay"></param>
        /// <param name="seriesConfig"></param>
        public SimpleMeasurementMetricConfiguration(
                                int seriesCountLimit,
                                int valuesPerDimensionLimit,
                                TimeSpan newSeriesCreationTimeout,
                                TimeSpan newSeriesCreationRetryDelay,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(seriesCountLimit));
            }

            if (valuesPerDimensionLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimit));
            }

            if (newSeriesCreationTimeout < TimeSpan.Zero || TimeSpan.FromSeconds(5) < NewSeriesCreationTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(newSeriesCreationTimeout));
            }

            if (newSeriesCreationRetryDelay < TimeSpan.Zero || TimeSpan.FromSeconds(1) < newSeriesCreationRetryDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(newSeriesCreationRetryDelay));
            }

            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));

            SeriesCountLimit = seriesCountLimit;
            ValuesPerDimensionLimit = valuesPerDimensionLimit;
            NewSeriesCreationTimeout = NewSeriesCreationTimeout;
            NewSeriesCreationRetryDelay = newSeriesCreationRetryDelay;
            SeriesConfig = seriesConfig;

            _hashCode = ComputeHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        public int SeriesCountLimit { get; }

        /// <summary>
        /// 
        /// </summary>
        public int ValuesPerDimensionLimit { get; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan NewSeriesCreationTimeout { get; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan NewSeriesCreationRetryDelay { get; }

        /// <summary>
        /// 
        /// </summary>
        public IMetricSeriesConfiguration SeriesConfig { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as SimpleMeasurementMetricConfiguration;
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
        public bool Equals(IMetricConfiguration other)
        {
            return Equals((object) other);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SimpleMeasurementMetricConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            return (this.SeriesCountLimit == other.SeriesCountLimit)
                && (this.ValuesPerDimensionLimit == other.ValuesPerDimensionLimit)
                && (this.NewSeriesCreationTimeout == other.NewSeriesCreationTimeout)
                && (this.NewSeriesCreationRetryDelay == other.NewSeriesCreationRetryDelay)
                && (this.SeriesConfig.Equals(other.SeriesConfig));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        private int ComputeHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + SeriesCountLimit.GetHashCode();
                hash = (hash * 23) + ValuesPerDimensionLimit.GetHashCode();
                hash = (hash * 23) + NewSeriesCreationTimeout.GetHashCode();
                hash = (hash * 23) + NewSeriesCreationRetryDelay.GetHashCode();
                hash = (hash * 23) + SeriesConfig.GetHashCode();
                return hash;
            }
        }
    }
}
