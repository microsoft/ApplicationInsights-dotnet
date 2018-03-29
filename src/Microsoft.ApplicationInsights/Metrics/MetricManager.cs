namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    /// <summary>@ToDo: Complete documentation before stable release. {529}</summary>
    public sealed class MetricManager
    {
        private readonly MetricAggregationManager aggregationManager;
        private readonly DefaultAggregationPeriodCycle aggregationCycle;
        private readonly IMetricTelemetryPipeline telemetryPipeline;
        private readonly MetricsCollection metrics;

        /// <summary>@ToDo: Complete documentation before stable release. {599}</summary>
        /// <param name="telemetryPipeline">@ToDo: Complete documentation before stable release. {795}</param>
        public MetricManager(IMetricTelemetryPipeline telemetryPipeline)
        {
            Util.ValidateNotNull(telemetryPipeline, nameof(telemetryPipeline));

            this.telemetryPipeline = telemetryPipeline;
            this.aggregationManager = new MetricAggregationManager();
            this.aggregationCycle = new DefaultAggregationPeriodCycle(this.aggregationManager, this);

            this.metrics = new MetricsCollection(this);

            this.aggregationCycle.Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MetricManager" /> class.
        /// </summary>
        ~MetricManager()
        {
            DefaultAggregationPeriodCycle aggregationCycle = this.aggregationCycle;
            if (aggregationCycle != null)
            {
                Task fireAndForget = this.aggregationCycle.StopAsync();
            }
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {328}</summary>
        public MetricsCollection Metrics
        {
            get { return this.metrics; }
        }

        internal MetricAggregationManager AggregationManager
        {
            get { return this.aggregationManager; }
        }

        internal DefaultAggregationPeriodCycle AggregationCycle
        {
            get { return this.aggregationCycle; }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {362}</summary>
        /// <param name="metricNamespace">@ToDo: Complete documentation before stable release. {953}</param>
        /// <param name="metricId">@ToDo: Complete documentation before stable release. {176}</param>
        /// <param name="config">@ToDo: Complete documentation before stable release. {016}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {996}</returns>
        public MetricSeries CreateNewSeries(string metricNamespace, string metricId, IMetricSeriesConfiguration config)
        {
            return this.CreateNewSeries(
                            metricNamespace,
                            metricId,
                            dimensionNamesAndValues: null,
                            config: config);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {064}</summary>
        /// <param name="metricNamespace">@ToDo: Complete documentation before stable release. {831}</param>
        /// <param name="metricId">@ToDo: Complete documentation before stable release. {381}</param>
        /// <param name="dimensionNamesAndValues">@ToDo: Complete documentation before stable release. {374}</param>
        /// <param name="config">@ToDo: Complete documentation before stable release. {303}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {866}</returns>
        public MetricSeries CreateNewSeries(
                                    string metricNamespace, 
                                    string metricId, 
                                    IEnumerable<KeyValuePair<string, string>> dimensionNamesAndValues, 
                                    IMetricSeriesConfiguration config)
        {
            // Create MetricIdentifier (it will also validate metricNamespace and metricId):
            List<string> dimNames = null;
            if (dimensionNamesAndValues != null)
            {
                dimNames = new List<string>();
                foreach (KeyValuePair<string, string> dimNameVal in dimensionNamesAndValues)
                {
                    dimNames.Add(dimNameVal.Key);
                }
            }

            var metricIdentifier = new MetricIdentifier(metricNamespace, metricId, dimNames);

            // Create series:
            return this.CreateNewSeries(metricIdentifier, dimensionNamesAndValues, config);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {569}</summary>
        /// <param name="metricIdentifier">@ToDo: Complete documentation before stable release. {108}</param>
        /// <param name="dimensionNamesAndValues">@ToDo: Complete documentation before stable release. {785}</param>
        /// <param name="config">@ToDo: Complete documentation before stable release. {275}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {908}</returns>
        public MetricSeries CreateNewSeries(MetricIdentifier metricIdentifier, IEnumerable<KeyValuePair<string, string>> dimensionNamesAndValues, IMetricSeriesConfiguration config)
        {
            Util.ValidateNotNull(metricIdentifier, nameof(metricIdentifier));
            Util.ValidateNotNull(config, nameof(config));

            var dataSeries = new MetricSeries(this.aggregationManager, metricIdentifier, dimensionNamesAndValues, config);
            return dataSeries;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {134}</summary>
        public void Flush()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            AggregationPeriodSummary aggregates = this.aggregationManager.StartOrCycleAggregators(MetricAggregationCycleKind.Default, futureFilter: null, tactTimestamp: now);
            this.TrackMetricAggregates(aggregates, flush: true);
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
                        Task trackTask = this.telemetryPipeline.TrackAsync(telemetryItem, CancellationToken.None);
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
                        Task trackTask = this.telemetryPipeline.TrackAsync(telemetryItem, CancellationToken.None);
                        trackTasks[taskIndex++] = trackTask;
                    }
                }
            }

            Task.WaitAll(trackTasks);

            if (flush)
            {
                Task flushTask = this.telemetryPipeline.FlushAsync(CancellationToken.None);
                flushTask.Wait();
            }
        }
    }        
}
