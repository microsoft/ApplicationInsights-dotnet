namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using static System.FormattableString;

    /// <summary>@ToDo: Complete documentation before stable release. {628}</summary>
    /// @PublicExposureCandidate
    internal class ApplicationInsightsTelemetryPipeline : IMetricTelemetryPipeline
    {
        private readonly ApplicationInsights.TelemetryClient trackingClient;
        private readonly Task completedTask = Task.FromResult(true);

        /// <summary>@ToDo: Complete documentation before stable release. {763}</summary>
        /// <param name="telemetryPipeline">@ToDo: Complete documentation before stable release. {887}</param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.Extensibility.TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            this.trackingClient = new ApplicationInsights.TelemetryClient(telemetryPipeline);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {253}</summary>
        /// <param name="telemetryClient">@ToDo: Complete documentation before stable release. {017}</param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.TelemetryClient telemetryClient)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            this.trackingClient = telemetryClient;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {017}</summary>
        /// <param name="metricAggregate">@ToDo: Complete documentation before stable release. {043}</param>
        /// <param name="cancelToken">@ToDo: Complete documentation before stable release. {921}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {373}</returns>
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
                throw new ArgumentException(Invariant($"Cannot track the specified {metricAggregate}, because there is no {nameof(IMetricAggregateToTelemetryPipelineConverter)}")
                                          + Invariant($" registered for it. A converter must be added to {nameof(MetricAggregateToTelemetryPipelineConverters)}")
                                          + Invariant($".{nameof(MetricAggregateToTelemetryPipelineConverters.Registry)} for the pipeline type")
                                          + Invariant($" '{typeof(ApplicationInsightsTelemetryPipeline).Name}' and {nameof(metricAggregate.AggregationKindMoniker)}")
                                          + Invariant($" '{metricAggregate.AggregationKindMoniker}'."));
            }

            object telemetryItem = converter.Convert(metricAggregate);
            var metricTelemetryItem = (ApplicationInsights.DataContracts.MetricTelemetry)telemetryItem;
            this.trackingClient.Track(metricTelemetryItem);

            return this.completedTask;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {935}</summary>
        /// <param name="cancelToken">@ToDo: Complete documentation before stable release. {490}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {817}</returns>
        public Task FlushAsync(CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();

            this.trackingClient.Flush();

            return this.completedTask;
        }
    }
}
