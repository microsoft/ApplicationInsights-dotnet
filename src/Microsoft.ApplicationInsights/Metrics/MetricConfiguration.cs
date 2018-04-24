namespace Microsoft.ApplicationInsights.Metrics
{
    using System;

    using static System.FormattableString;

    /// <summary>@ToDo: Complete documentation before stable release. {654}</summary>
    public class MetricConfiguration : IEquatable<MetricConfiguration>
    {
        private readonly int hashCode;

        /// <summary>@ToDo: Complete documentation before stable release. {477}</summary>
        /// <param name="seriesCountLimit">@ToDo: Complete documentation before stable release. {815}</param>
        /// <param name="valuesPerDimensionLimit">@ToDo: Complete documentation before stable release. {426}</param>
        /// <param name="seriesConfig">@ToDo: Complete documentation before stable release. {618}</param>
        public MetricConfiguration(
                                int seriesCountLimit,
                                int valuesPerDimensionLimit,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(
                                                    nameof(seriesCountLimit),
                                                    Invariant($"Metrics must allow at least one data series (but {seriesCountLimit} was specified)."));
            }

            if (valuesPerDimensionLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimit));
            }
           
            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));

            this.SeriesCountLimit = seriesCountLimit;
            this.ValuesPerDimensionLimit = valuesPerDimensionLimit;

            this.SeriesConfig = seriesConfig;

            this.hashCode = Util.CombineHashCodes(
                                        this.SeriesCountLimit.GetHashCode(),
                                        this.ValuesPerDimensionLimit.GetHashCode(),
                                        this.SeriesConfig.GetType().FullName.GetHashCode(),
                                        this.SeriesConfig.GetHashCode());
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {215}</summary>
        public int SeriesCountLimit { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {311}</summary>
        public int ValuesPerDimensionLimit { get; }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {528}</summary>
        public IMetricSeriesConfiguration SeriesConfig { get; }

        /// <summary>@ToDo: Complete documentation before stable release. {611}</summary>
        /// <param name="obj">@ToDo: Complete documentation before stable release. {066}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {342}</returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricConfiguration;
                if (otherConfig != null)
                {
                    return this.Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {321}</summary>
        /// <param name="other">@ToDo: Complete documentation before stable release. {605}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {731}</returns>
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
                && this.GetType().Equals(other.GetType())
                && this.SeriesConfig.Equals(other.SeriesConfig);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {852}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {895}</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }
    }
}
