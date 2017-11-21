using System;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetricSeriesConfiguration : IEquatable<IMetricSeriesConfiguration>
    {
        /// <summary>
        /// </summary>
        bool RequiresPersistentAggregation { get; }

        /// <summary>
        /// </summary>
        bool AutoCleanupUnusedSeries { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSeries"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <returns></returns>
        IMetricSeriesAggregator CreateNewAggregator(MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind);
    }
}
