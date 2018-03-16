using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricConfigurationForNaiveDistinctCount : MetricConfiguration
    {
        /// <summary />
        /// <param name="seriesCountLimit"></param>
        /// <param name="valuesPerDimensionLimit"></param>
        /// <param name="seriesConfig"></param>
        public MetricConfigurationForNaiveDistinctCount(
                                            int seriesCountLimit, 
                                            int valuesPerDimensionLimit, 
                                            MetricSeriesConfigurationForNaiveDistinctCount seriesConfig)
            : base(seriesCountLimit, valuesPerDimensionLimit, seriesConfig)
        {
        }
    }
}
