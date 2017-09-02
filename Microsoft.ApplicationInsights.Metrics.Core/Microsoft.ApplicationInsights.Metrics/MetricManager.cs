using System;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
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
        private readonly TelemetryClient _trackingClient;

        private object _metricCache;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryPipeline"></param>
        public MetricManager(TelemetryConfiguration telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            _trackingClient = new TelemetryClient(telemetryPipeline);
            _aggregationManager = new MetricAggregationManager();
            _aggregationCycle = new DefaultAggregationPeriodCycle(_aggregationManager, this);

            _aggregationCycle.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        ~MetricManager()
        {
            var fireAndForget = this.StopAsync();
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

            var dataSeries = new MetricSeries(_aggregationManager, metricId, config);
            return dataSeries;
        }

        /// <summary>
        /// 
        /// </summary>
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

        internal object GetOrCreateCacheUnsafe(Func<MetricManager, object> newCacheInstanceFactory)
        {
            object cache = _metricCache;

            if (cache != null)
            {
                return cache;
            }

            Util.ValidateNotNull(newCacheInstanceFactory, nameof(newCacheInstanceFactory));

            object newCache = null;
            try
            {
                newCache = newCacheInstanceFactory(this);
            }
            catch
            {
                newCache = null;
            }

            if (newCache != null)
            {
                object prevCache = Interlocked.CompareExchange(ref _metricCache, newCache, null);
                cache = prevCache ?? newCache;
            }

            return cache;
        }
    }
}
