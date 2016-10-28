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
        /// A dictionary of all metric aggregators instantiated via this manager.
        /// </summary>
        private ConcurrentDictionary<string, MetricAggregator> aggregatorDictionary;

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

            this.lastSnapshotStartDateTime = DateTimeOffset.UtcNow;
            this.aggregatorDictionary = new ConcurrentDictionary<string, MetricAggregator>();

            this.cancellationSource = new CancellationTokenSource();

            this.snapshotTask = new Task(this.SnapshotRunner, TaskCreationOptions.LongRunning);
            this.snapshotTask.Start();
        }

        /// <summary>
        /// Returns single time series metric data aggregator.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="dimensions">Optional dimensions.</param>
        /// <returns>Value aggregator for the metric specified.</returns>
        public MetricAggregator GetMetricAggregator(string metricName, IDictionary<string, string> dimensions = null)
        {
            // get aggregator id to use as a key to find the aggregator if one exists
            string aggregatorId = MetricAggregator.GetAggregatorId(metricName, dimensions);

            return this.aggregatorDictionary.GetOrAdd(
                aggregatorId, 
                (aid) => { return new MetricAggregator(this.client.TelemetryConfiguration, metricName, dimensions); });
        }

        /// <summary>
        /// Flushes the in-memory aggregation buffers.
        /// </summary>
        public void Flush()
        {
            try
            {
                this.Snapshot();
                this.client.Flush();
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
        /// <returns>Metric telemetry object resulting from aggregation.</returns>
        private static AggregatedMetricTelemetry CreateAggergatedMetricTelemetry(MetricAggregator aggregator)
        {
            if ((aggregator == null) || (aggregator.Count <= 0))
            {
                return null;
            }

            var telemetry = new AggregatedMetricTelemetry(
                aggregator.MetricName,
                aggregator.Count,
                aggregator.Sum,
                aggregator.Min,
                aggregator.Max,
                aggregator.StandardDeviation);

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
            ConcurrentDictionary<string, MetricAggregator> aggregatorSnapshot =
                Interlocked.Exchange(ref this.aggregatorDictionary, new ConcurrentDictionary<string, MetricAggregator>());

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
                foreach (MetricAggregator aggregator in aggregatorSnapshot.Values)
                {
                    AggregatedMetricTelemetry aggergatedMetricTelemetry = CreateAggergatedMetricTelemetry(aggregator);

                    aggergatedMetricTelemetry.Properties.Add(intervalDurationPropertyName, ((long)aggregationIntervalDuation.TotalMilliseconds).ToString(CultureInfo.InvariantCulture));

                    if (aggergatedMetricTelemetry != null)
                    {
                        this.client.Track(aggergatedMetricTelemetry);
                    }
                }
            }
        }
    }
}
