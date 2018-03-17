using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricConfigurationForGauge : MetricConfiguration
    {
        /// <summary />
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="seriesConfig"></param>
        public MetricConfigurationForGauge(int seriesCountLimit, int valuesPerDimensionLimit, MetricSeriesConfigurationForGauge seriesConfig)
            : base(seriesCountLimit, valuesPerDimensionLimit, seriesConfig)
        {
        }
    }
}
