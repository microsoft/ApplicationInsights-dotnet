using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetricSeriesConfiguration : IEquatable<IMetricSeriesConfiguration>
    {
        /// <summary>
        /// 
        /// </summary>
        bool RequiresPersistentAggregation { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="consumerKind"></param>
        /// <returns></returns>
        IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricConsumerKind consumerKind);
    }
}
