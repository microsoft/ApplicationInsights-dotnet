using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleMetricConfiguration : IMetricConfiguration
    {
        private readonly int _hashCode;

        /// <summary />
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="seriesConfig"></param>
        public SimpleMetricConfiguration(
                                int seriesCountLimit,
                                int valuesPerDimensionLimit,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(
                                                    nameof(seriesCountLimit),
                                                    $"Metrics must allow at least one data series (but {seriesCountLimit} was specified).");
            }

            if (valuesPerDimensionLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimit));
            }
           
            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));

            SeriesCountLimit = seriesCountLimit;
            ValuesPerDimensionLimit = valuesPerDimensionLimit;

            SeriesConfig = seriesConfig;

            _hashCode = ComputeHashCode();
        }

        /// <summary />
        public int SeriesCountLimit { get; }

        /// <summary />
        public int ValuesPerDimensionLimit { get; }

        /// <summary />
        public IMetricSeriesConfiguration SeriesConfig { get; }

        /// <summary />
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as SimpleMetricConfiguration;
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
        public bool Equals(IMetricConfiguration other)
        {
            return Equals((object) other);
        }

        /// <summary />
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SimpleMetricConfiguration other)
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
                && (this.SeriesConfig.Equals(other.SeriesConfig));
        }

        /// <summary />
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
                hash = (hash * 23) + SeriesConfig.GetType().FullName.GetHashCode();
                hash = (hash * 23) + SeriesConfig.GetHashCode();
                return hash;
            }
        }
    }
}
