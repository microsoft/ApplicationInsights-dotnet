using System;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public class SimpleMeasurementMetricConfiguration : IMetricConfiguration
    {
        public int SeriesCountLimit { get; }

        public int ValuesPerDimensionLimit { get; }

        public TimeSpan NewSeriesCreationTimeout { get; }

        public TimeSpan NewSeriesCreationRetryDelay { get; }

        public IMetricSeriesConfiguration SeriesConfig { get; }

        public SimpleMeasurementMetricConfiguration(int seriesCountLimit, int valuesPerDimensionLimit, IMetricSeriesConfiguration seriesConfig)
            : this(seriesCountLimit,
                  valuesPerDimensionLimit,
                  MetricConfiguration.Defaults.NewSeriesCreationTimeout,
                  MetricConfiguration.Defaults.NewSeriesCreationRetryDelay,
                  seriesConfig)
        {
        }

        public SimpleMeasurementMetricConfiguration(int seriesCountLimit,
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
        }

        public override bool Equals(object other)
        {
            if (other != null)
            {
                var otherConfig = other as SimpleMeasurementMetricConfiguration;
                if (otherConfig != null)
                {
                    return Equals(otherConfig);
                }
            }

            return false;
        }

        public bool Equals(IMetricConfiguration other)
        {
            return Equals((object) other);
        }

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
    }
}
