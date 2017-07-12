using System;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    public sealed class MetricManager
    {
        private readonly MetricAggregationManager _aggregationManager;
        private readonly DefaultAggregationPeriodCycle _aggregationCycle;
        private readonly TelemetryClient _trackingClient;

        internal MetricAggregationManager AggregationManager { get { return _aggregationManager; } }

        public MetricManager(TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            _trackingClient = new TelemetryClient(telemetryPipeline);
            _aggregationManager = new MetricAggregationManager();
            _aggregationCycle = new DefaultAggregationPeriodCycle(_aggregationManager, this);

            _aggregationCycle.Start();
        }

        ~MetricManager()
        {
            Task fireAndForget = StopAsync();
        }

        public MetricDataSeries CreateNewDataSeries(string metricId, IMetricConfiguration config)
        {
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(config, nameof(config));

            var dataSeries = new MetricDataSeries(_aggregationManager, metricId, config);
            return dataSeries;
        }


        /// <summary>
        /// Metric Manager does not encapsulate any disposable or native resourses. However, it encapsulates a managed thread.
        /// In normal cases, a metric manager is accessed via convenience methods and consumers never need to worry about that thread.
        /// However, advanced scenarios may explicitly create a metric manager instance. In such cases, consumers may may need to call
        /// this method on the explicitly created instance to let the thread know that it no longer needs to run. The thread will not
        /// be aborted proactively. Instead, it will complete the ongoing aggregation cycle and gracfully exit instead of scheduling
        /// the next iteration. However, the background thread will not send any aggregated metrics if it has been notified to stop.
        /// Therefore, this method flushed current data before sending the notification.
        /// </summary>
        /// <returns>
        /// You can await the returned Task if you want to be sure that the encapsulated thread completed.
        /// If you just want to notify the thread to stop without waiting for it, do d=not await this method.
        /// </returns>
        public Task StopAsync()
        {
            Flush();
            return _aggregationCycle.StopAsync();
        }


        public void Flush()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            AggregationPeriodSummary aggregates = _aggregationManager.CycleAggregators(MetricConsumerKind.Default, updatedFilter: null, tactTimestamp: now);
            TrackMetricAggregates(aggregates);
        }


        internal void TrackMetricAggregates(AggregationPeriodSummary aggregates)
        {
            if (aggregates == null)
            {
                return;
            }

            if (aggregates.FilteredAggregates != null && aggregates.FilteredAggregates.Count != 0)
            {
                foreach (ITelemetry telemetryItem in aggregates.FilteredAggregates)
                {
                    if (telemetryItem != null)
                    {
                        _trackingClient.Track(telemetryItem);
                    }
                }
            }

            if (aggregates.UnfilteredValuesAggregates != null && aggregates.UnfilteredValuesAggregates.Count != 0)
            {
                foreach (ITelemetry telemetryItem in aggregates.UnfilteredValuesAggregates)
                {
                    if (telemetryItem != null)
                    {
                        _trackingClient.Track(telemetryItem);
                    }
                }
            }
        }
    }
}
