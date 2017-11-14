using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// </summary>
    public class ApplicationInsightsTelemetryPipeline : IMetricTelemetryPipeline
    {
        private readonly ApplicationInsights.TelemetryClient _trackingClient;
        private readonly Task _completedTask = Task.FromResult(true);

        /// <summary />
        /// <param name="telemetryPipeline"></param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.Extensibility.TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            _trackingClient = new ApplicationInsights.TelemetryClient(telemetryPipeline);
        }

        /// <summary />
        /// <param name="telemetryClient"></param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.TelemetryClient telemetryClient)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            _trackingClient = telemetryClient;
        }

        /// <summary />
        /// <param name="metricAggregate"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
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
            if (! hasConverter)
            {
                throw new ArgumentException($"Cannot track the specified {metricAggregate}, because there is no {nameof(IMetricAggregateToTelemetryPipelineConverter)}"
                                          + $" registered for it. A converter must be added to {nameof(MetricAggregateToTelemetryPipelineConverters)}"
                                          + $".{nameof(MetricAggregateToTelemetryPipelineConverters.Registry)} for the pipeline type"
                                          + $" '{typeof(ApplicationInsightsTelemetryPipeline).Name}' and {nameof(metricAggregate.AggregationKindMoniker)}"
                                          + $" '{metricAggregate.AggregationKindMoniker}'.");
            }

            object telemetryItem = converter.Convert(metricAggregate);
            var metricTelemetryItem = (ApplicationInsights.DataContracts.MetricTelemetry) telemetryItem;
            _trackingClient.Track(metricTelemetryItem);

            return _completedTask;
        }

        /// <summary />
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task FlushAsync(CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();
            _trackingClient.Flush();
            return _completedTask;
        }
    }
}
