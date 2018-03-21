namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    using TaskEx = System.Threading.Tasks.Task;

    /// <summary>
    /// Metric factory and controller. Sends metrics to Application Insights service. Pre-aggregates metrics to reduce bandwidth.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#send-metrics">Learn more</a>
    /// </summary>
    internal sealed class MetricManagerV1 : IDisposable
    {
        /// <summary>
        /// Value of the property indicating 'app insights version' allowing to tell metric was built using metric manager.
        /// </summary>
        private static string sdkVersionPropertyValue = SdkVersionUtils.GetSdkVersion("m-agg:");

        /// <summary>
        /// Reporting frequency.
        /// </summary>
        private static TimeSpan aggregationPeriod = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Telemetry client used to track resulting aggregated metrics.
        /// </summary>
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Metric aggregation snapshot task.
        /// </summary>
        private TaskTimerInternal snapshotTimer;

        /// <summary>
        /// Last time snapshot was initiated.
        /// </summary>
        private DateTimeOffset lastSnapshotStartDateTime;

        /// <summary>
        /// A dictionary of all metrics instantiated via this manager.
        /// </summary>
        private ConcurrentDictionary<MetricV1, SimpleMetricStatisticsAggregator> metricDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricManagerV1"/> class.
        /// </summary>
        public MetricManagerV1()
            : this(new TelemetryClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricManagerV1"/> class.
        /// </summary>
        /// <param name="client">Telemetry client to use to output aggregated metric data.</param>
        public MetricManagerV1(TelemetryClient client)
        {
            this.telemetryClient = client ?? new TelemetryClient();

            this.metricDictionary = new ConcurrentDictionary<MetricV1, SimpleMetricStatisticsAggregator>();

            this.lastSnapshotStartDateTime = DateTimeOffset.UtcNow;

            this.snapshotTimer = new TaskTimerInternal() { Delay = GetWaitTime() };
            this.snapshotTimer.Start(this.SnapshotAndReschedule);
        }

        /// <summary>
        /// Gets a list of metric processors associated
        /// with this instance of <see cref="MetricManagerV1"/>.
        /// </summary>
        internal IList<IMetricProcessorV1> MetricProcessors
        {
            get
            {
                TelemetryConfiguration config = this.telemetryClient.TelemetryConfiguration;

                return config.MetricProcessors;
            }
        }

        /// <summary>
        /// Creates metric.
        /// </summary>
        /// <param name="name">Name of the metric.</param>
        /// <param name="dimensions">Optional dimensions.</param>
        /// <returns>Metric instance.</returns>
        /// <remarks>
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#send-metrics">Learn more</a>
        /// </remarks>
        public MetricV1 CreateMetric(string name, IDictionary<string, string> dimensions = null)
        {
            return new MetricV1(this, name, dimensions);
        }

        /// <summary>
        /// Flushes the in-memory aggregation buffers. Not normally required - occurs automatically at intervals and on Dispose.
        /// </summary>
        public void Flush()
        {
            try
            {
                this.Snapshot();
                this.telemetryClient.Flush();
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.FailedToFlushMetricAggregators(ex.ToString());
            }
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.snapshotTimer.Dispose();
            this.Flush();
        }

        internal SimpleMetricStatisticsAggregator GetStatisticsAggregator(MetricV1 metric)
        {
            return this.metricDictionary.GetOrAdd(metric, (m) => { return new SimpleMetricStatisticsAggregator(); });
        }

        /// <summary>
        /// Calculates wait time until next snapshot of the aggregators.
        /// </summary>
        /// <returns>Wait time.</returns>
        private static TimeSpan GetWaitTime()
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;

            double minutesFromZero = currentTime.Subtract(DateTimeOffset.MinValue).TotalMinutes;

            // we want to wake up exactly at 1 second past minute
            // to make perceived system latency look smaller
            var nextWakeTime = DateTimeOffset.MinValue
                .AddMinutes((long)minutesFromZero)
                .Add(aggregationPeriod)
                .AddSeconds(1);

            TimeSpan sleepTime = nextWakeTime - DateTimeOffset.UtcNow;

            // adjust wait time to a bit longer than a minute if the wake up time is within few seconds from now
            return sleepTime < TimeSpan.FromSeconds(3) ? sleepTime.Add(aggregationPeriod) : sleepTime;
        }

        /// <summary>
        /// Generates telemetry object based on the metric aggregator.
        /// </summary>
        /// <param name="metric">Metric definition.</param>
        /// <param name="statistics">Metric aggregator statistics calculated for a period of time.</param>
        /// <returns>Metric telemetry object resulting from aggregation.</returns>
        private static MetricTelemetry CreateAggregatedMetricTelemetry(MetricV1 metric, SimpleMetricStatisticsAggregator statistics)
        {
            var telemetry = new MetricTelemetry(
                metric.Name,
                statistics.Count,
                statistics.Sum,
                statistics.Min,
                statistics.Max,
                statistics.StandardDeviation);

            if (metric.Dimensions != null)
            {
                foreach (KeyValuePair<string, string> property in metric.Dimensions)
                {
                    telemetry.Properties.Add(property);
                }
            }

            // add a header allowing to distinguish metrics
            // built using metric manager from other metrics
            telemetry.Context.GetInternalContext().SdkVersion = sdkVersionPropertyValue;
            
            return telemetry;
        }

        /// <summary>
        /// Takes a snapshot of aggregators collected by this instance of the manager
        /// and schedules the next snapshot.
        /// </summary>
        private Task SnapshotAndReschedule()
        {
            return Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        this.Snapshot();
                    }
                    catch (Exception ex)
                    {
                        CoreEventSource.Log.FailedToSnapshotMetricAggregators(ex.ToString());
                    }
                    finally
                    {
                        this.snapshotTimer.Delay = GetWaitTime();
                        this.snapshotTimer.Start(this.SnapshotAndReschedule);
                    }
                });
        }

        /// <summary>
        /// Takes snapshot of all active metric aggregators and turns results into metric telemetry.
        /// </summary>
        private void Snapshot()
        {
            ConcurrentDictionary<MetricV1, SimpleMetricStatisticsAggregator> aggregatorSnapshot =
                Interlocked.Exchange(ref this.metricDictionary, new ConcurrentDictionary<MetricV1, SimpleMetricStatisticsAggregator>());

            // calculate aggregation interval duration interval
            TimeSpan aggregationIntervalDuation = DateTimeOffset.UtcNow - this.lastSnapshotStartDateTime;
            this.lastSnapshotStartDateTime = DateTimeOffset.UtcNow;

            // prevent zero duration for interval
            if (aggregationIntervalDuation.TotalMilliseconds < 1)
            {
                aggregationIntervalDuation = TimeSpan.FromMilliseconds(1);
            }

            // adjust interval duration to exactly snapshot frequency if it is close (within 1%)
            double difference = Math.Abs(aggregationIntervalDuation.TotalMilliseconds - aggregationPeriod.TotalMilliseconds);

            if (difference <= aggregationPeriod.TotalMilliseconds / 100)
            {
                aggregationIntervalDuation = aggregationPeriod;
            }

            if (aggregatorSnapshot.Count > 0)
            {
                foreach (KeyValuePair<MetricV1, SimpleMetricStatisticsAggregator> aggregatorWithStats in aggregatorSnapshot)
                {
                    if (aggregatorWithStats.Value.Count > 0)
                    { 
                        MetricTelemetry aggregatedMetricTelemetry = CreateAggregatedMetricTelemetry(aggregatorWithStats.Key, aggregatorWithStats.Value);

                        aggregatedMetricTelemetry.Properties.Add(
                                                        MetricTerms.Aggregation.Interval.Moniker.Key,
                                                        ((long)aggregationIntervalDuation.TotalMilliseconds).ToString(CultureInfo.InvariantCulture));

                        // set the timestamp back by aggregation period
                        aggregatedMetricTelemetry.Timestamp = DateTimeOffset.Now - aggregationPeriod;

                        this.telemetryClient.Track(aggregatedMetricTelemetry);
                    }
                }
            }
        }
    }
}
