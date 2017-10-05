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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <returns></returns>
        public static AggregationPeriodSummary StopAggregators(
                                                        this MetricManager metricManager,
                                                        MetricAggregationCycleKind aggregationCycleKind,
                                                        DateTimeOffset tactTimestamp)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.StopAggregators(aggregationCycleKind, tactTimestamp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricManager"></param>
        /// <param name="aggregationCycleKind"></param>
        /// <param name="tactTimestamp"></param>
        /// <param name="futureFilter"></param>
        /// <returns></returns>
        public static AggregationPeriodSummary StartOrCycleAggregators(
                                                        this MetricManager metricManager,
                                                        MetricAggregationCycleKind aggregationCycleKind,
                                                        DateTimeOffset tactTimestamp,
                                                        IMetricSeriesFilter futureFilter)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.StartOrCycleAggregators(aggregationCycleKind, tactTimestamp, futureFilter);
        }

        /// <summary>
        /// Stops metric aggregation in advanced scenarios where a MetricManager was explicitly created using its ctor.
        /// </summary>
        /// <remarks>
        /// Metric Manager does not encapsulate any disposable or native resourses. However, it encapsulates a managed thread.
        /// In normal cases, a metric manager is accessed via convenience methods and consumers never need to worry about that thread.
        /// However, advanced scenarios may explicitly create a metric manager instance. In such cases, consumers may need to call
        /// this method on the explicitly created instance to let the thread know that it no longer needs to run. The thread will not
        /// be aborted proactively. Instead, it will complete the ongoing aggregation cycle and then gracfully exit instead of scheduling
        /// the next iteration. However, the background thread will not send any aggregated metrics if it has been notified to stop.
        /// Therefore, this method flushes current data before sending the notification.
        /// </remarks>
        /// <param name="metricManager">The metric manager</param>
        /// <returns>
        /// You can await the returned Task if you want to be sure that the encapsulated thread completed.
        /// If you just want to notify the thread to stop without waiting for it, do not await this method.
        /// </returns>
        public static Task StopAsync(this MetricManager metricManager)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));

            metricManager.Flush();
            return metricManager.AggregationCycle.StopAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="metricManager"></param>
        /// <param name="newExtensionStateInstanceFactory"></param>
        /// <returns></returns>
        public static T GetOrCreateExtensionState<T>(this MetricManager metricManager, Func<MetricManager, T> newExtensionStateInstanceFactory)
                where T : class
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));

            object cache =  metricManager.GetOrCreateExtensionStateUnsafe(newExtensionStateInstanceFactory);

            if (cache == null)
            {
                return null;
            }

            T typedCache = cache as T;
            if (typedCache == null)
            {
                throw new InvalidOperationException($"{nameof(MetricManagerExtensions)}.{nameof(GetOrCreateExtensionState)}<T>(..) expected to find a"
                                                  + $" cache of type {typeof(T).FullName}, but the present cache was of"
                                                  + $" type {cache.GetType().FullName}. This indicates that multiple extensions attempt to use"
                                                  + $" this extension point of the {nameof(MetricManager)} in a conflicting manner.");
            }

            return typedCache;
        }
    }
}
