namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if CORE_PCL || NET45 || NET46
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Represents a hub for metric aggregation procedures.
    /// </summary>
    public sealed class MetricAggregatorManager : IDisposable
    {
        /// <summary>
        /// Name of the property added to aggregation results to indicate duration of the aggregation interval.
        /// </summary>
        private static string intervalDurationPropertyName = "IntervalDurationMs";

        /// <summary>
        /// Reporting frequency.
        /// </summary>
        private static TimeSpan snapshotFrequency = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Last time snapshot was initiated.
        /// </summary>
        private DateTimeOffset lastSnapshotStartDateTime;

        /// <summary>
        /// Cancellation token source to allow cancellation of the snapshotting task.
        /// </summary>
        private CancellationTokenSource cancellationSource;

        /// <summary>
        /// Metric aggregator snapshotting task.
        /// </summary>
        private Task snapshotTask;

        /// <summary>
        /// A dictionary of all metric aggregators instantiated via this manager.
        /// </summary>
        private ConcurrentDictionary<MetricAggregator, SimpleMetricStatisticsAggregator> aggregatorDictionary;

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
            this.Client = client ?? new TelemetryClient();
            this.aggregatorDictionary = new ConcurrentDictionary<MetricAggregator, SimpleMetricStatisticsAggregator>();

            this.lastSnapshotStartDateTime = DateTimeOffset.UtcNow;

            this.cancellationSource = new CancellationTokenSource();

            this.snapshotTask = new Task(this.SnapshotRunner, TaskCreationOptions.LongRunning);
            this.snapshotTask.Start();
        }

        /// <summary>
        /// Gets telemetry client used to output aggregation results.
        /// </summary>
        internal TelemetryClient Client { get; private set; }

        /// <summary>
        /// Creates single time series metric data aggregator.
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
            try
            {
                this.Snapshot();
                this.Client.Flush();
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

        internal SimpleMetricStatisticsAggregator GetStatisticsAggregator(MetricAggregator aggregator)
        {
            if (aggregator == null)
            {
                return null;
            }

            return this.aggregatorDictionary.GetOrAdd(aggregator, (aid) => { return new SimpleMetricStatisticsAggregator(); });
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
                    // note: we need to flush under catch since ThreadAbortException 
                    // is re-thrown after the try block
                    this.Flush();
                    break;
                }
#endif
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    CoreEventSource.Log.FailedToSnapshotMetricAggregators(ex.ToString());
                }
            }

            // relying in the fact that Flush() suppresses exceptions
            this.Flush();
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
                .Add(snapshotFrequency)
                .AddSeconds(1);

            TimeSpan sleepTime = nextWakeTime - DateTimeOffset.UtcNow;

            // adjust wait time to a bit longer than a minute if the wake up time is within few seconds from now
            return sleepTime < TimeSpan.FromSeconds(3) ? sleepTime.Add(snapshotFrequency) : sleepTime;
        }

        /// <summary>
        /// Generates telemetry object based on the metric aggregator.
        /// </summary>
        /// <param name="aggregator">Metric aggregator.</param>
        /// <param name="aggregatorStats">Metric aggregator statistics calculated for a period of time.</param>
        /// <returns>Metric telemetry object resulting from aggregation.</returns>
        private static AggregatedMetricTelemetry CreateAggergatedMetricTelemetry(MetricAggregator aggregator, SimpleMetricStatisticsAggregator aggregatorStats)
        {
            if ((aggregator == null) || (aggregatorStats == null) || (aggregatorStats.Count <= 0))
            {
                return null;
            }

            var telemetry = new AggregatedMetricTelemetry(
                aggregator.MetricName,
                aggregatorStats.Count,
                aggregatorStats.Sum,
                aggregatorStats.Min,
                aggregatorStats.Max,
                aggregatorStats.StandardDeviation);

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
            ConcurrentDictionary<MetricAggregator, SimpleMetricStatisticsAggregator> aggregatorSnapshot =
                Interlocked.Exchange(ref this.aggregatorDictionary, new ConcurrentDictionary<MetricAggregator, SimpleMetricStatisticsAggregator>());

            // calculate aggregation interval duration interval
            TimeSpan aggregationIntervalDuation = DateTimeOffset.UtcNow - this.lastSnapshotStartDateTime;
            this.lastSnapshotStartDateTime = DateTimeOffset.UtcNow;

            // prevent zero duration for interval
            if (aggregationIntervalDuation.TotalMilliseconds < 1)
            {
                aggregationIntervalDuation = TimeSpan.FromMilliseconds(1);
            }

            // adjust interval duration to exactly snapshot frequency if it is close (within 1%)
            double difference = Math.Abs(aggregationIntervalDuation.TotalMilliseconds - snapshotFrequency.TotalMilliseconds);

            if (difference <= snapshotFrequency.TotalMilliseconds / 100)
            {
                aggregationIntervalDuation = snapshotFrequency;
            }

            if (aggregatorSnapshot.Count > 0)
            {
                foreach (KeyValuePair<MetricAggregator, SimpleMetricStatisticsAggregator> aggregatorWithStats in aggregatorSnapshot)
                {
                    AggregatedMetricTelemetry aggergatedMetricTelemetry = CreateAggergatedMetricTelemetry(aggregatorWithStats.Key, aggregatorWithStats.Value);

                    aggergatedMetricTelemetry.Properties.Add(intervalDurationPropertyName, ((long)aggregationIntervalDuation.TotalMilliseconds).ToString(CultureInfo.InvariantCulture));

                    if (aggergatedMetricTelemetry != null)
                    {
                        this.Client.Track(aggergatedMetricTelemetry);
                    }
                }
            }
        }
    }
}
