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

        /// <summary>
        /// </summary>
        /// <param name="telemetryPipeline"></param>
        public ApplicationInsightsTelemetryPipeline(ApplicationInsights.Extensibility.TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            _trackingClient = new ApplicationInsights.TelemetryClient(telemetryPipeline);
        }

        /// <summary>
        /// </summary>
        /// <param name="metricAggregate"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task TrackAsync(object metricAggregate, CancellationToken cancelToken)
        {
            Util.ValidateNotNull(metricAggregate, nameof(metricAggregate));

            var telemetryItem = metricAggregate as ApplicationInsights.Channel.ITelemetry;
            if (telemetryItem == null)
            {
                throw new ArgumentException($"This instance of {nameof(IMetricTelemetryPipeline)} is of runtime class {nameof(ApplicationInsightsTelemetryPipeline)}."
                                          + $" It can only track metric aggregates of type {nameof(Microsoft.ApplicationInsights.Channel.ITelemetry)}."
                                          + $" However, the specified {nameof(metricAggregate)} is on runtime type {metricAggregate.GetType().Name}.");
            }

            cancelToken.ThrowIfCancellationRequested();

            _trackingClient.Track(telemetryItem);

            return _completedTask;
        }

        /// <summary>
        /// </summary>
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
