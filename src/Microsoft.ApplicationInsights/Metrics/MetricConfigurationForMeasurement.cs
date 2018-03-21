namespace Microsoft.ApplicationInsights.Metrics
{
    using System;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public sealed class MetricConfigurationForMeasurement : MetricConfiguration
    {
        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="seriesCountLimit">ToDo: Complete documentation before stable release.</param>
        /// <param name="valuesPerDimensionLimit">ToDo: Complete documentation before stable release.</param>
        /// <param name="seriesConfig">ToDo: Complete documentation before stable release.</param>
        public MetricConfigurationForMeasurement(int seriesCountLimit, int valuesPerDimensionLimit, MetricSeriesConfigurationForMeasurement seriesConfig)
            : base(seriesCountLimit, valuesPerDimensionLimit, seriesConfig)
        {
        }
    }
}
