using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// There are some methods on that MetricManager needs to forward to its encapsulated MetricAggregationManager that need to be public.
    /// However, in order not to pulute the API surface shown by Intellisense, we redirect them through this class, which is located in a more specialized namespace.
    /// </summary>
    public static class MetricManagerExtensions
    {
        public static bool StartAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, IMetricDataSeriesFilter filter, DateTimeOffset tactTimestamp)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.AggregationManager.StartAggregators(consumerKind, filter, tactTimestamp);
        }

        public static AggregationPeriodSummary StopAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, DateTimeOffset tactTimestamp)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.StopAggregators(consumerKind, tactTimestamp);
        }

        public static AggregationPeriodSummary CycleAggregators(this MetricManager metricManager, MetricConsumerKind consumerKind, IMetricDataSeriesFilter updatedFilter, DateTimeOffset tactTimestamp)
        {
            Util.ValidateNotNull(metricManager, nameof(metricManager));
            return metricManager.CycleAggregators(consumerKind, updatedFilter, tactTimestamp);
        }
    }
}
