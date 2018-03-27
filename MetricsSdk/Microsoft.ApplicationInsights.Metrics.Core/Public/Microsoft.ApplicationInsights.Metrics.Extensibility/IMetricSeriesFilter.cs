using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetricSeriesFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="valueFilter"></param>
        /// <returns></returns>
        bool WillConsume(MetricSeries dataSeries, out IMetricValueFilter valueFilter);
    }
}