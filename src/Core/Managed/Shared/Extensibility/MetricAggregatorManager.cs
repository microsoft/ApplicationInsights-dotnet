namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if CORE_PCL || NET45 || NET46
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Represents a hub for metric aggregation procedures.
    /// </summary>
    public sealed class MetricAggregatorManager : IDisposable
    {
        /// <summary>
        /// Reporting frequency.
        /// </summary>
        private static TimeSpan snapshotFrequency = TimeSpan.FromMinutes(1);

        /// <summary>
        /// A dictionary of all metric aggregators instantiated via this manager.
        /// </summary>
        private ConcurrentDictionary<MetricAggregator, SimpleMetricStatsAggregator> aggregatorDictionary;

        /// <summary>
        /// Cancellation token source to allow cancellation of the snapshotting task.
        /// </summary>
        private CancellationTokenSource cancellationSource;

        /// <summary>
        /// Metric aggregator snapshotting task.
        /// </summary>
        private Task snapshotTask;

        /// <summary>
        /// Telemetry client used to output aggregation results.
        /// </summary>
        private TelemetryClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregatorManager"/> class.
        /// </summary>
        public MetricAggregatorManager()
            : this(new TelemetryClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregatorManager"/> class.
        /// </summary>
        /// <param name="client">Telemetry client to use to output aggregated metric data.</param>
        public MetricAggregatorManager(TelemetryClient client)
        {
            this.client = client ?? new TelemetryClient();

            this.aggregatorDictionary = new ConcurrentDictionary<MetricAggregator, SimpleMetricStatsAggregator>();

            this.cancellationSource = new CancellationTokenSource();

            this.snapshotTask = new Task(this.SnapshotRunner, TaskCreationOptions.LongRunning);
            this.snapshotTask.Start();
        }

        /// <summary>
        /// Creates single time series data aggregator.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="dimensions">Optional dimensions.</param>
        /// <returns>Value aggregator for the metric specified.</returns>
        public MetricAggregator CreateMetricAggregator(string metricName, IDictionary<string, string> dimensions = null)
        {
            return new MetricAggregator(this, metricName, dimensions);
        }

        /// <summary>
        /// Flushes the in-memory aggregation buffers.
        /// </summary>
        public void Flush()
        {
            this.Snapshot();
            this.client.Flush();
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            if (this.cancellationSource != null)
            {
                this.cancellationSource.Cancel();

                if (this.snapshotTask != null)
                {
                    this.snapshotTask.Wait();
#if NET40 || NET45 || NET46
                    this.snapshotTask.Dispose();
#endif
                }

                this.cancellationSource.Dispose();
            }
        }

        /// <summary>
        /// Gets metric statistics set based on the aggregator.
        /// </summary>
        /// <param name="aggregator">Metric aggregator.</param>
        /// <returns>Metric statistics.</returns>
        internal SimpleMetricStatsAggregator GetSimpleStatisticsAggregator(MetricAggregator aggregator)
        {
            return this.aggregatorDictionary.GetOrAdd(aggregator, (a) => { return new SimpleMetricStatsAggregator(); });
        }

        /// <summary>
        /// Represents long running task to periodically snapshot metric aggregators.
        /// </summary>
        internal async void SnapshotRunner()
        {
            while (!this.cancellationSource.Token.IsCancellationRequested)
            {
                try
                {
                    await TaskEx.Delay(GetWaitTime(), this.cancellationSource.Token).ConfigureAwait(false);

                    this.Snapshot();
                }
#if NET40 || NET45 || NET46
                catch (ThreadAbortException)
                {
                    break;
                }
#endif
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // TODO log exception and try to survive
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }

            try
            {
                this.Flush();
            }
            catch (Exception ex)
            {
                // TODO log exception and suppress
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Forwards metric sample to metric processors.
        /// </summary>
        /// <param name="metricName">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="dimensions">Optional metric dimensions.</param>
        internal void ForwardToProcessors(string metricName, double value, IDictionary<string, string> dimensions = null)
        {
            TelemetryConfiguration configuration = this.client.TelemetryConfiguration;

            // create a local reference to metric processor collection
            // if collection changes after that - it will be copied not affecting local reference
            IList<IMetricProcessor> metricProcessors = configuration.MetricProcessors;

            if (metricProcessors != null)
            {
                foreach (IMetricProcessor processor in metricProcessors)
                {
                    try
                    {
                        processor.Track(metricName, value, dimensions);
                    }
                    catch (Exception ex)
                    {
                        // TODO log exception and suppress
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Calculates wait time until next snapshot of the aggregators.
        /// </summary>
        /// <returns>Wait time.</returns>
        private static TimeSpan GetWaitTime()
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;

            double minutesFromZero = currentTime.Subtract(DateTimeOffset.MinValue).TotalMinutes;

            // we want to wake up exactly at the middle of the minute
            var nextWakeTime = DateTimeOffset.MinValue.AddMinutes((long)minutesFromZero).AddSeconds(30);

            TimeSpan sleepTime = nextWakeTime - DateTimeOffset.UtcNow;

            // adjust wait time to a bit longer than a minute if the wake up time is within few seconds from now
            return sleepTime < TimeSpan.FromSeconds(3) ? sleepTime.Add(snapshotFrequency) : sleepTime;
        }

        /// <summary>
        /// Generates telemetry object based on the metric aggregator.
        /// </summary>
        /// <param name="aggregator">Metric aggregator.</param>
        /// <param name="stats">Aggregation statistics.</param>
        /// <returns>Metric telemetry object resulting from aggregation.</returns>
        private static ITelemetry CreateAggergatedMetricTelemetry(MetricAggregator aggregator, SimpleMetricStatsAggregator stats)
        {
            if ((aggregator == null) || (stats == null) || (stats.Count <= 0))
            {
                return null;
            }

            var telemetry = new AggregatedMetricTelemetry(
                aggregator.MetricName,
                stats.Count,
                stats.Sum,
                stats.Min,
                stats.Max,
                stats.StandardDeviation);

            if (aggregator.Dimensions != null)
            {
                foreach (KeyValuePair<string, string> property in aggregator.Dimensions)
                {
                    telemetry.Properties.Add(property);
                }
            }

            return telemetry;
        }

        /// <summary>
        /// Takes snapshot of all active metric aggregators and turns results into metric telemetry.
        /// </summary>
        private void Snapshot()
        {
            ConcurrentDictionary<MetricAggregator, SimpleMetricStatsAggregator> aggregatorSnapshot =
                Interlocked.Exchange(ref this.aggregatorDictionary, new ConcurrentDictionary<MetricAggregator, SimpleMetricStatsAggregator>());

            if (aggregatorSnapshot.Count > 0)
            {
                foreach (KeyValuePair<MetricAggregator, SimpleMetricStatsAggregator> metricStats in aggregatorSnapshot)
                {
                    ITelemetry aggergatedMetricTelemetry = CreateAggergatedMetricTelemetry(metricStats.Key, metricStats.Value);

                    if (aggergatedMetricTelemetry != null)
                    {
                        this.client.Track(aggergatedMetricTelemetry);
                    }
                }
            }
        }
    }
}
