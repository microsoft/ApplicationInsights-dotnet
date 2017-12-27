using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MetricManager
    {
        private readonly MetricAggregationManager _aggregationManager;
        private readonly DefaultAggregationPeriodCycle _aggregationCycle;
        private readonly IMetricTelemetryPipeline _telemetryPipeline;
        private readonly MetricsCollection _metrics;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryPipeline"></param>
        public MetricManager(IMetricTelemetryPipeline telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            _telemetryPipeline = telemetryPipeline;
            _aggregationManager = new MetricAggregationManager();
            _aggregationCycle = new DefaultAggregationPeriodCycle(_aggregationManager, this);

            _metrics = new MetricsCollection(this);

            _aggregationCycle.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        ~MetricManager()
        {
            DefaultAggregationPeriodCycle aggregationCycle = _aggregationCycle;
            if (aggregationCycle != null)
            {
                Task fireAndForget = _aggregationCycle.StopAsync();
            }
        }

        /// <summary>
        /// </summary>
        public MetricsCollection Metrics { get { return _metrics; } }

        internal MetricAggregationManager AggregationManager { get { return _aggregationManager; } }

        internal DefaultAggregationPeriodCycle AggregationCycle { get { return _aggregationCycle; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricId"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public MetricSeries CreateNewSeries(string metricId, IMetricSeriesConfiguration config)
        {
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(config, nameof(config));

            var dataSeries = new MetricSeries(_aggregationManager, metricId, null, config);
            return dataSeries;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metricId"></param>
        /// <param name="dimensionNamesAndValues"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public MetricSeries CreateNewSeries(string metricId, IEnumerable<KeyValuePair<string, string>> dimensionNamesAndValues, IMetricSeriesConfiguration config)
        {
            Util.ValidateNotNull(metricId, nameof(metricId));
            Util.ValidateNotNull(config, nameof(config));

            var dataSeries = new MetricSeries(_aggregationManager, metricId, dimensionNamesAndValues, config);
            return dataSeries;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            AggregationPeriodSummary aggregates = _aggregationManager.StartOrCycleAggregators(MetricAggregationCycleKind.Default, futureFilter: null, tactTimestamp: now);
            TrackMetricAggregates(aggregates, flush: true);
        }


        internal void TrackMetricAggregates(AggregationPeriodSummary aggregates, bool flush)
        {
            int nonpersistentAggregatesCount = (aggregates?.NonpersistentAggregates == null)
                                                    ? 0
                                                    : aggregates.NonpersistentAggregates.Count;

            int persistentAggregatesCount = (aggregates?.PersistentAggregates == null)
                                                    ? 0
                                                    : aggregates.PersistentAggregates.Count;

            int totalAggregatesCount = nonpersistentAggregatesCount + persistentAggregatesCount;
            if (totalAggregatesCount == 0)
            {
                return;
            }

            Task[] trackTasks = new Task[totalAggregatesCount];
            int taskIndex = 0;

            if (nonpersistentAggregatesCount != 0)
            {
                foreach (MetricAggregate telemetryItem in aggregates.NonpersistentAggregates)
                {
                    if (telemetryItem != null)
                    {
                        Task trackTask = _telemetryPipeline.TrackAsync(telemetryItem, CancellationToken.None);
                        trackTasks[taskIndex++] = trackTask;
                    }
                }
            }

            if (aggregates.PersistentAggregates != null && aggregates.PersistentAggregates.Count != 0)
            {
                foreach (MetricAggregate telemetryItem in aggregates.PersistentAggregates)
                {
                    if (telemetryItem != null)
                    {
                        Task trackTask = _telemetryPipeline.TrackAsync(telemetryItem, CancellationToken.None);
                        trackTasks[taskIndex++] = trackTask;
                    }
                }
            }

            Task.WaitAll(trackTasks);

            if (flush)
            {
                Task flushTask = _telemetryPipeline.FlushAsync(CancellationToken.None);
                flushTask.Wait();
            }
        }
    }        
}
