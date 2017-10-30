using System;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        private object _extensionState;
        
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

        internal object GetOrCreateExtensionStateUnsafe(Func<MetricManager, object> newExtensionStateInstanceFactory)
        {
            object extensionState = _extensionState;

            if (extensionState != null)
            {
                return extensionState;
            }

            Util.ValidateNotNull(newExtensionStateInstanceFactory, nameof(newExtensionStateInstanceFactory));

            object newExtensionState = null;
            try
            {
                newExtensionState = newExtensionStateInstanceFactory(this);
            }
            catch
            {
                newExtensionState = null;
            }

            if (newExtensionState != null)
            {
                object prevExtensionState = Interlocked.CompareExchange(ref _extensionState, newExtensionState, null);
                extensionState = prevExtensionState ?? newExtensionState;
            }

            return extensionState;
        }
    }
}
