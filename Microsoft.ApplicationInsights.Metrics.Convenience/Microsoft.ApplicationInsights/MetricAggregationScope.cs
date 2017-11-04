using System;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Used when creating a <see cref="Metric" /> to specify scope across which the values for the metric are to be aggregated in memory.
    /// </summary>
    public enum MetricAggregationScope
    {
        /// <summary>
        /// Specifies that the <see cref="Microsoft.ApplicationInsights.Metrics.MetricManager" /> instance that owns a <c>Metric</c> will
        /// be attached to a <c>TelemetryConfiguration</c> instance.
        /// Metric values will be aggregated across ALL telemetry clients that belong to the same <c>TelemetryConfiguration</c>.
        /// As a result, when metric values are tracked using the <c>.GetMetric(..)</c> extension method of a <c>TelemetryClient</c>,
        /// the <c>Context</c>, the <c>InstrumentationKey</c> and other properties of the respective <c>TelemetryClient</c> will be ignored
        /// in favor of the <c>TelemetryConfiguration</c>-wide settings.
        /// This is the default and preferred option. It fits most use cases and is more conservative towards resources.
        /// </summary>
        TelemetryConfiguration = 1,

        /// Specifies that the <see cref="Microsoft.ApplicationInsights.Metrics.MetricManager" /> instance that owns a <c>Metric</c> will
        /// be attached to a specified <c>TelemetryClient</c> instance.
        /// Metric values will be aggregated only for a specific <c>TelemetryClient</c> instance and then sent using that particular instance.
        /// As a result, the <c>Context</c> and the <c>InstrumentationKey</c> of the specified <c>telemetryClient</c> will be respected.
        /// Note that aggregation across many smaller scopes can be resource intensive. Each <c>MetricManager</c> instance holds a managed thread
        /// and each aggregator uses additional memory. Using this option is only recommended when an application requires a particular instance
        /// of <c>TelementryClient</c> to be used for sending telemetry.
        TelemetryClient = 2,
    }
}
