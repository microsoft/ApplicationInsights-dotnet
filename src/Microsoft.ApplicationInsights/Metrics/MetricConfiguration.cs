namespace Microsoft.ApplicationInsights.Metrics
{
    using System;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public class MetricConfiguration : IEquatable<MetricConfiguration>
    {
        private readonly int _hashCode;

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="seriesCountLimit">ToDo: Complete documentation before stable release.</param>
        /// <param name="valuesPerDimensionLimit">ToDo: Complete documentation before stable release.</param>
        /// <param name="seriesConfig">ToDo: Complete documentation before stable release.</param>
        public MetricConfiguration(
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

            _hashCode = Util.CombineHashCodes(
                                        SeriesCountLimit.GetHashCode(),
                                        ValuesPerDimensionLimit.GetHashCode(),
                                        SeriesConfig.GetType().FullName.GetHashCode(),
                                        SeriesConfig.GetHashCode());
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int SeriesCountLimit { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int ValuesPerDimensionLimit { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public IMetricSeriesConfiguration SeriesConfig { get; }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="obj">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricConfiguration;
                if (otherConfig != null)
                {
                    return Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="other">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public virtual bool Equals(MetricConfiguration other)
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
                && (this.GetType().Equals(other.GetType()))
                && (this.SeriesConfig.Equals(other.SeriesConfig));
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
