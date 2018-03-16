using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricConfigurationForMeasurement : MetricConfiguration
    {
        /// <summary />
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="seriesConfig"></param>
        public MetricConfigurationForMeasurement(int seriesCountLimit, int valuesPerDimensionLimit, MetricSeriesConfigurationForMeasurement seriesConfig)
            : base(seriesCountLimit, valuesPerDimensionLimit, seriesConfig)
        {
        }
    }
}
