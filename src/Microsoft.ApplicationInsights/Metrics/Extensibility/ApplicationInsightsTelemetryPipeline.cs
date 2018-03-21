namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public class ApplicationInsightsTelemetryPipeline : IMetricTelemetryPipeline
    {
        private readonly ApplicationInsights.TelemetryClient trackingClient;
        private readonly Task completedTask = Task.FromResult(true);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="telemetryPipeline">ToDo: Complete documentation before stable release.</param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.Extensibility.TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            this.trackingClient = new ApplicationInsights.TelemetryClient(telemetryPipeline);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="telemetryClient">ToDo: Complete documentation before stable release.</param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.TelemetryClient telemetryClient)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            this.trackingClient = telemetryClient;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricAggregate">ToDo: Complete documentation before stable release.</param>
        /// <param name="cancelToken">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public Task TrackAsync(MetricAggregate metricAggregate, CancellationToken cancelToken)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));
            Util.ValidateNotNull(metricAggregate.AggregationKindMoniker, nameof(metricAggregate.AggregationKindMoniker));

            cancelToken.ThrowIfCancellationRequested();

            IMetricAggregateToTelemetryPipelineConverter converter;
            bool hasConverter = MetricAggregateToTelemetryPipelineConverters.Registry.TryGet(
                                                                                            typeof(ApplicationInsightsTelemetryPipeline),
                                                                                            metricAggregate.AggregationKindMoniker,
                                                                                            out converter);
            if (false == hasConverter)
            {
                throw new ArgumentException($"Cannot track the specified {metricAggregate}, because there is no {nameof(IMetricAggregateToTelemetryPipelineConverter)}"
                                          + $" registered for it. A converter must be added to {nameof(MetricAggregateToTelemetryPipelineConverters)}"
                                          + $".{nameof(MetricAggregateToTelemetryPipelineConverters.Registry)} for the pipeline type"
                                          + $" '{typeof(ApplicationInsightsTelemetryPipeline).Name}' and {nameof(metricAggregate.AggregationKindMoniker)}"
                                          + $" '{metricAggregate.AggregationKindMoniker}'.");
            }

            object telemetryItem = converter.Convert(metricAggregate);
            var metricTelemetryItem = (ApplicationInsights.DataContracts.MetricTelemetry)telemetryItem;
            this.trackingClient.Track(metricTelemetryItem);

            return this.completedTask;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="cancelToken">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public Task FlushAsync(CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();
            try
            {
                this.trackingClient.Flush();
            }
            catch (NullReferenceException)
            {
                // If the user has disposed the pipeline and we are subsequently completing the last aggregation cycle, the above can throw.
            }

            return this.completedTask;
        }
    }
}
