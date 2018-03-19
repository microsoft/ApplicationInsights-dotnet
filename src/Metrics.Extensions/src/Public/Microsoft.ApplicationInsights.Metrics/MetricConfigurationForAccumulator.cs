using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricConfigurationForAccumulator : MetricConfiguration
    {
        /// <summary />
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="seriesConfig"></param>
        public MetricConfigurationForAccumulator(int seriesCountLimit, int valuesPerDimensionLimit, MetricSeriesConfigurationForAccumulator seriesConfig)
            : base(seriesCountLimit, valuesPerDimensionLimit, seriesConfig)
        {
        }
    }
}
