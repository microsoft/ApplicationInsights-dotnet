namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetricValueFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="metricValue"></param>
        /// <returns></returns>
        bool WillConsume(MetricSeries dataSeries, uint metricValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="metricValue"></param>
        /// <returns></returns>
        bool WillConsume(MetricSeries dataSeries, double metricValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="metricValue"></param>
        /// <returns></returns>
        bool WillConsume(MetricSeries dataSeries, object metricValue);
    }
}