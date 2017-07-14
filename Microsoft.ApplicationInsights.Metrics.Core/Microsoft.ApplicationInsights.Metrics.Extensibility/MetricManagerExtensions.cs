using System;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// There are some methods on that MetricManager needs to forward to its encapsulated MetricAggregationManager that need to be public.
    /// However, in order not to pulute the API surface shown by Intellisense, we redirect them through this class, which is located in a more specialized namespace.
    /// </summary>
    public static class MetricManagerExtensions
    {
        public static bool StartAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter filter)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.StartAggregators(consumerKind, tactTimestamp, filter);
        }

        public static AggregationPeriodSummary StopAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.StopAggregators(consumerKind, tactTimestamp);
        }

        public static AggregationPeriodSummary CycleAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp, IMetricSeriesFilter updatedFilter)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.CycleAggregators(consumerKind, tactTimestamp, updatedFilter);
        }

        /// <summary>
        /// Stops metric aggregation in advanced scenarios where a MetricManager was explicitly created using its ctor.
        /// </summary>
        /// <remarks>
        /// Metric Manager does not encapsulate any disposable or native resourses. However, it encapsulates a managed thread.
        /// In normal cases, a metric manager is accessed via convenience methods and consumers never need to worry about that thread.
        /// However, advanced scenarios may explicitly create a metric manager instance. In such cases, consumers may may need to call
        /// this method on the explicitly created instance to let the thread know that it no longer needs to run. The thread will not
        /// be aborted proactively. Instead, it will complete the ongoing aggregation cycle and gracfully exit instead of scheduling
        /// the next iteration. However, the background thread will not send any aggregated metrics if it has been notified to stop.
        /// Therefore, this method flushed current data before sending the notification.
        /// </remarks>
        /// <returns>
        /// You can await the returned Task if you want to be sure that the encapsulated thread completed.
        /// If you just want to notify the thread to stop without waiting for it, do d=not await this method.
        /// </returns>
        public static Task StopAsync(this MetricManager metricManager)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));

            metricManager.Flush();
            return metricManager.AggregationCycle.StopAsync();
        }
    }
}
