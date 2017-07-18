using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class DefaultAggregationPeriodCycle
    {
        private const int RunningState_NotStarted = 0;
        private const int RunningState_Running = 1;
        private const int RunningState_Stopped = 2;

        private int _runningState;
        private Task _workerTask = null;

        private readonly Action _workerMethod;

        private readonly MetricAggregationManager _aggregationManager;
        private readonly MetricManager _metricManager;

        public DefaultAggregationPeriodCycle(MetricAggregationManager aggregationManager, MetricManager metricManager)
        {
            Util.ValidateNotNull(aggregationManager, nameof(aggregationManager));
            Util.ValidateNotNull(metricManager, nameof(metricManager));

            _workerMethod = this.Run;

            _aggregationManager = aggregationManager;
            _metricManager = metricManager;
            _runningState = RunningState_NotStarted;
        }

        public bool Start()
        {
            int prev = Interlocked.CompareExchange(ref _runningState, RunningState_Running, RunningState_NotStarted);

            if (prev != RunningState_NotStarted)
            {
                return false; // Was already running or stopped.
            }

            _workerTask = Task.Run(_workerMethod);
            return true;
        }

        public async Task StopAsync()
        {
            int prev = Interlocked.CompareExchange(ref _runningState, RunningState_Stopped, RunningState_Running);

            if (prev == RunningState_NotStarted)
            {
                return;
            }

            // Benign race on being called very soon after start. Will miss a cycle but eventually complete correctly.

            Task workerTask = _workerTask;
            if (workerTask != null)
            {
                await _workerTask;
                _workerTask = null;
            }
        }

        /// <summary>
        /// We use exactly one background thread for completing aggregators - either once per minute or once per second.
        /// We start this thread right when this manager is created to avoid that potential thread starvation on busy systems affects metrics.
        /// </summary>
        private void Run()
        {
            while (true)
            {
                DateTimeOffset now = DateTimeOffset.Now;
                TimeSpan waitPeriod = GetNextCycleTargetTime(now) - now;

                //Thread.Sleep(waitPeriod);
                Task.Delay(waitPeriod).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();

                int shouldBeRunning = Volatile.Read(ref _runningState);
                if (shouldBeRunning != RunningState_Running)
                {
                    return;
                }

                FetchAndTrackMetrics();
            }
        }

        public void FetchAndTrackMetrics()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            AggregationPeriodSummary aggregates = _aggregationManager.CycleAggregators(MetricConsumerKind.Default, updatedFilter: null, tactTimestamp: now);
            if (aggregates != null)
            {
                Task fireAndForget = Task.Run(() => _metricManager.TrackMetricAggregates(aggregates));
            }
        }

        private DateTimeOffset GetNextCycleTargetTime(DateTimeOffset periodStart)
        {
            // Next tick: (current time rounded down to MINUTE start) + (1 minute) + (small sub-minute offset).
            // The strategy here is to always "tick" at the same offset within a minute.
            // Due to drift and inprecize timing this may conflict with the aggregation being exactly a minute.
            // In such cases we err on the side of keeping the same offset.
            // This will tend to straighten out the inmterval and to yield consistent timestamps.

            const int targetOffsetFromRebasedCurrentTimeInSecs = (60) + 1;
            const double minPeriodInSecs = 20.0;

            DateTimeOffset target = new DateTimeOffset(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, 0, periodStart.Offset)
                                        .AddSeconds(targetOffsetFromRebasedCurrentTimeInSecs);

            // If this results in the next period being unreasonably short, we extend that period by 1 minute,
            // resulting in a total period that is somewhat longer than a minute.

            TimeSpan waitPeriod = target - periodStart;
            if (waitPeriod.TotalSeconds < minPeriodInSecs)
            {
                target = target.AddMinutes(1);
            }

            return target;
        }
    }

}