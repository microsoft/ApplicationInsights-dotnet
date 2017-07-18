using System;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public interface IMetricConfiguration : IEquatable<IMetricConfiguration>
    {
        int SeriesCountLimit { get; }
        int ValuesPerDimensionLimit { get; }

        TimeSpan NewSeriesCreationTimeout { get; }
        TimeSpan NewSeriesCreationRetryDelay { get; }

        IMetricSeriesConfiguration SeriesConfig { get; }
    }
}