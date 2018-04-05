namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// There are some methods on that MetricManager needs to forward to its encapsulated MetricAggregationManager that need to be public.
    /// However, in order not to pulute the API surface shown by Intellisense, we redirect them through this class, which is located in a more specialized namespace.
    /// </summary>
    /// @PublicExposureCandidate
    internal static class MetricManagerExtensions
    {
        /// <summary>@ToDo: Complete documentation before stable release. {989}</summary>
        /// <param name="metricManager">@ToDo: Complete documentation before stable release. {335}</param>
        /// <param name="aggregationCycleKind">@ToDo: Complete documentation before stable release. {001}</param>
        /// <param name="tactTimestamp">@ToDo: Complete documentation before stable release. {687}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {620}</returns>
        public static AggregationPeriodSummary StopAggregators(
                                                        this MetricManager metricManager,
                                                        MetricAggregationCycleKind aggregationCycleKind,
                                                        DateTimeOffset tactTimestamp)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.StopAggregators(aggregationCycleKind, tactTimestamp);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {396}</summary>
        /// <param name="metricManager">@ToDo: Complete documentation before stable release. {784}</param>
        /// <param name="aggregationCycleKind">@ToDo: Complete documentation before stable release. {805}</param>
        /// <param name="tactTimestamp">@ToDo: Complete documentation before stable release. {879}</param>
        /// <param name="futureFilter">@ToDo: Complete documentation before stable release. {735}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {762}</returns>
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
        public static Task StopDefaultAggregationCycleAsync(this MetricManager metricManager)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));

            metricManager.Flush();
            return metricManager.AggregationCycle.StopAsync();
        }
    }
}
