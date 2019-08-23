namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility;

    using static System.FormattableString;

    /// <summary>Metric related extension methods for the <c>TelemetryClient</c>.
    /// Note that these APIs are in the ...Extensibility namespace and do not pollute the API surface for users who do not import it.</summary>
    public static class TelemetryClientExtensions
    {
        /// <summary>Gets the <c>MetricManager</c> for this <c>TelemetryClient</c> at the specified scope.
        /// If a metric manager does not exist at the specified scope, it is created.</summary>
        /// <param name="telemetryClient">The telemetry client for which to get the metric manager.</param>
        /// <param name="aggregationScope">If <c>MetricAggregationScope.TelemetryClient</c> is specified,
        /// the metric manager specific to this client is returned. Such manager aggregates metrics for this
        /// client object only. Two metrics with exactly the same id, namespace and dimensions would be aggregated
        /// separately for different telemetry client objects when this scope is used.<br />
        /// If <c>MetricAggregationScope.TelemetryConfiguration</c> is specified,
        /// the metric manager for the telemetry configuration of this client is returned. Such manager aggregates
        /// metrics for all clients that use that telemetry configuration. Two metrics with exactly the same id,
        /// namespace and dimensions would be aggregated together for different telemetry client objects that use
        /// the same telemetry configuration when this scope is used. <br/>
        /// <seealso cref="MetricAggregationScope"/>
        /// </param>
        /// <returns>The metric manager for this telemetry client at the specified scope.</returns>
        public static MetricManager GetMetricManager(this TelemetryClient telemetryClient, MetricAggregationScope aggregationScope)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            switch (aggregationScope)
            {
                case MetricAggregationScope.TelemetryConfiguration:
                    TelemetryConfiguration pipeline = telemetryClient.TelemetryConfiguration;
                    return pipeline.GetMetricManager();

                case MetricAggregationScope.TelemetryClient:
                    MetricManager manager = telemetryClient.GetOrCreateMetricManager();
                    return manager;

                default:
                    throw new ArgumentException(Invariant($"Invalid value of {nameof(aggregationScope)} ({aggregationScope}).")
                                              + Invariant($" Only the following values are supported:")
                                              + Invariant($" ['{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryClient.ToString()}',")
                                              + Invariant($" '{nameof(MetricAggregationScope)}.{MetricAggregationScope.TelemetryConfiguration.ToString()}']."));
            }
        }
    }
}
