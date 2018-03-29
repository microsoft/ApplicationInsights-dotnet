namespace Microsoft.ApplicationInsights.Metrics
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release. {218}</summary>
    public sealed class MetricConfigurationForMeasurement : MetricConfiguration
    {
        /// <summary>@ToDo: Complete documentation before stable release. {928}</summary>
        /// <param name="seriesCountLimit">@ToDo: Complete documentation before stable release. {466}</param>
        /// <param name="valuesPerDimensionLimit">@ToDo: Complete documentation before stable release. {730}</param>
        /// <param name="seriesConfig">@ToDo: Complete documentation before stable release. {977}</param>
        public MetricConfigurationForMeasurement(int seriesCountLimit, int valuesPerDimensionLimit, MetricSeriesConfigurationForMeasurement seriesConfig)
            : base(seriesCountLimit, valuesPerDimensionLimit, seriesConfig)
        {
        }
    }
}
