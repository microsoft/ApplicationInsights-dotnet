using System;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMetricSeriesAggregator
    {
        /// <summary>
        /// 
        /// </summary>
        DateTimeOffset PeriodStart { get; }

        /// <summary>
        /// 
        /// </summary>
        DateTimeOffset PeriodEnd { get; }

        /// <summary>
        /// 
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 
        /// </summary>
        MetricSeries DataSeries { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool TryRecycle();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodStart"></param>
        /// <param name="valueFilter"></param>
        void ReinitializePeriodAndAggregatedValues(DateTimeOffset periodStart, IMetricValueFilter valueFilter);

        /// <summary>
        /// 
        /// </summary>
        void ReinitializeAggregatedValues();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodEnd"></param>
        /// <returns></returns>
        ITelemetry CompleteAggregation(DateTimeOffset periodEnd);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="periodEnd"></param>
        /// <returns></returns>
        ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        void TrackValue(double metricValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricValue"></param>
        void TrackValue(object metricValue);
    }
}
