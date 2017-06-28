using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    public class MetricManager2
    {
        private const int TickMillisOffsetWithinSecond = 10;
        private const int TickMillisOffsetWithinMinute = 1000;

        private static TimeSpan MinSecondPeriod = TimeSpan.FromMilliseconds(330);
        private static TimeSpan MinMinutePeriod = TimeSpan.FromSeconds(20);

        private static TimeSpan SecondPeriodDriftTolerance = TimeSpan.FromMilliseconds(8);
        private static TimeSpan MinutePeriodDriftTolerance = TimeSpan.FromMilliseconds(400);

        public class MetricConsumersCollection
        {
            private readonly MetricManager _metricManager;
            private IMetricConsumer _minuteAggregationsConsumer = null;
            private IMetricConsumer _secondAggregationsConsumer = null;
            private IMetricConsumer _rawDataConsumer = null;

            public IMetricConsumer MinuteAggregation { get { return _minuteAggregationsConsumer; } set { _minuteAggregationsConsumer = value; _metricManager.MetricConsumersUpdated(); } }
            public IMetricConsumer SecondAggregation { get { return _secondAggregationsConsumer; } set { _secondAggregationsConsumer = value; _metricManager.MetricConsumersUpdated(); } }
            public IMetricConsumer RawData           { get { return _rawDataConsumer; }            set { _rawDataConsumer = value; _metricManager.MetricConsumersUpdated(); } }
            internal MetricConsumersCollection(MetricManager metricManager)
            {
                _metricManager = metricManager;
            }
        }

        private class MetricAggregatorCollection
        {
            public DateTimeOffset PeriodStart { get; }
            public IList<IMetricDataSeriesAggregator> Aggregators { get; }

            public MetricAggregatorCollection(DateTimeOffset periodStart)
            {
                this.PeriodStart = periodStart;
                this.Aggregators = new List<IMetricDataSeriesAggregator>();
            }
        }

        private MetricAggregatorCollection _secondAggregators;
        private MetricAggregatorCollection _minuteAggregators;

        private ManualResetEvent _aggregatorCompletionWaitEvent;
        
        public MetricConsumersCollection MetricConsumers { get; }

        public MetricManager2()
        {
            this.MetricConsumers = new MetricConsumersCollection(this);

            _aggregatorCompletionWaitEvent = new ManualResetEvent(false);
        }

        public void MetricConsumersUpdated()
        {
            ManualResetEvent aggregatorCompletionWaitEvent = _aggregatorCompletionWaitEvent;
            if (aggregatorCompletionWaitEvent != null)
            {
                aggregatorCompletionWaitEvent.Set();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            CompleteAggregators(ref _secondAggregators, MetricConsumers.SecondAggregation, utcNow);
            CompleteAggregators(ref _minuteAggregators, MetricConsumers.SecondAggregation, utcNow);
        }

        /// <summary>
        /// We use exactly one background thread for completing aggregators - either once per minute or once per second.
        /// We start this thread right when this manager is created to avoid that potential thread starvation on busy systems affects metrics.
        /// </summary>
        private void AggregatorsCompletionCycle()
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;

            while (true)
            {
                TimeSpan waitPeriod = (this.MetricConsumers.SecondAggregation != null)
                                            ? GetNextCompletionTargetForSecondAggregators(utcNow) - utcNow
                                            : GetNextCompletionTargetForMinuteAggregators(utcNow) - utcNow;

                bool waitInterupted = WaitForNextAggregatorsCompletionCycle(waitPeriod);

                utcNow = DateTimeOffset.UtcNow;

                if (_aggregatorCompletionWaitEvent == null)
                {
                    Flush();
                    return;
                }

                if (NeedsCompleteSecondAggregators(utcNow))
                {
                    CompleteAggregators(ref _secondAggregators, MetricConsumers.SecondAggregation, utcNow);
                }

                if (NeedsCompleteMinuteAggregators(utcNow))
                {
                    CompleteAggregators(ref _minuteAggregators, MetricConsumers.MinuteAggregation, utcNow);
                }
            }
        }

        

        private void CompleteAggregators(ref MetricAggregatorCollection completingAggregators, IMetricConsumer metricConsumer, DateTimeOffset utcNow)
        {
            MetricAggregatorCollection nextAggregators = new MetricAggregatorCollection(utcNow);
            MetricAggregatorCollection prevAggragators = Interlocked.Exchange(ref completingAggregators, nextAggregators);

            for (int i = 0; i < prevAggragators.Aggregators.Count; i++)
            {
                IMetricDataSeriesAggregator prevAggregator = prevAggragators.Aggregators[i];
                MetricDataSeries dataSeries = prevAggregator.MetricDataSeries;

                bool consumerInterest = metricConsumer?.IsInterestedIn(dataSeries) ?? false;
                bool needsRatainState = prevAggregator.NeedsRetainState;

                if (consumerInterest || needsRatainState)
                {
                    ITelemetry aggregatedDataSeries = prevAggregator.Complete(utcNow);

                    if (consumerInterest)
                    {
                        metricConsumer.TrackAggregation(aggregatedDataSeries);
                    }

                    if (needsRatainState)
                    {
                        IMetricDataSeriesAggregator nextAggregator = dataSeries.GetAggregator();
                        nextAggregator.TrackPreviousState(prevAggregator);
                    }
                }
            }
        }

        private bool NeedsCompleteSecondAggregators(DateTimeOffset utcNow)
        {
            DateTimeOffset targetFlushTime = GetNextCompletionTargetForSecondAggregators(_secondAggregators.PeriodStart);

            return (targetFlushTime <= utcNow)   // If we are past the target time, we should complete
                        ||
                    ((targetFlushTime - utcNow).Duration() <= SecondPeriodDriftTolerance);  // if we are very close to the target time, we should also complete
        }

        private bool NeedsCompleteMinuteAggregators(DateTimeOffset utcNow)
        {
            DateTimeOffset targetFlushTime = GetNextCompletionTargetForMinuteAggregators(_minuteAggregators.PeriodStart);

            return (targetFlushTime <= utcNow)   // If we are past the target time, we should complete
                        ||
                    ((targetFlushTime - utcNow).Duration() <= MinutePeriodDriftTolerance);  // if we are very close to the target time, we should also complete
        }

        private DateTimeOffset GetNextCompletionTargetForSecondAggregators(DateTimeOffset periodStart)
        {
            // The strategy here is to always "tick" at the same offset within a minute (if no by-second aggregation consumers are present)
            // or at the same offset within a second if by-second consumers are present.
            // In rare cases this may conflict with the aggregation being exactly a minute (or a second).
            // In such cases we err on the side of keeping the same offset.
            // This will tend to straighten out the inmterval and to yield consistent timestamps.

            // Per-second aggregations are consumed.
            // Next tick: (current time rounded down to SECOND start) + (1 second) + (small sub-second offset)

            const int targetOffsetFromRebasedCurrentTime = 1000 + TickMillisOffsetWithinSecond;

            DateTimeOffset target = new DateTimeOffset(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, periodStart.Second, periodStart.Offset)
                                    .AddMilliseconds(targetOffsetFromRebasedCurrentTime);

            // If this results in the next period being unreasonably short, we extend that period by 1 second,
            // resulting in a total period that is somewhat longer than a second.

            TimeSpan waitPeriod = target - periodStart;
            if (waitPeriod < MinSecondPeriod)
            {
                target = new DateTimeOffset(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, periodStart.Second, periodStart.Offset)
                            .AddMilliseconds(1000 + targetOffsetFromRebasedCurrentTime);
            }

            return target;
        }

        private DateTimeOffset GetNextCompletionTargetForMinuteAggregators(DateTimeOffset periodStart)
        {
            // The strategy here is to always "tick" at the same offset within a minute (if no by-second aggregation consumers are present)
            // or at the same offset within a second if by-second consumers are present.
            // In rare cases this may conflict with the aggregation being exactly a minute (or a second).
            // In such cases we err on the side of keeping the same offset.
            // This will tend to straighten out the inmterval and to yield consistent timestamps.

            // Per-second aggregations are NOT consumed.
            // Next tick: (current time rounded down to MINUTE start) + (1 minute) + (small sub-minute offset)

            const int targetOffsetFromRebasedCurrentTime = (60 * 1000) + TickMillisOffsetWithinMinute;

            DateTimeOffset target = new DateTimeOffset(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, 0, periodStart.Offset)
                                    .AddMilliseconds(targetOffsetFromRebasedCurrentTime);

            // If this results in the next period being unreasonably short, we extend that period by 1 minute,
            // resulting in a total period that is somewhat longer than a minute.

            TimeSpan waitPeriod = target - periodStart;
            if (waitPeriod < MinMinutePeriod)
            {
                target = new DateTimeOffset(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, 0, periodStart.Offset)
                            .AddMilliseconds((60 * 1000) + targetOffsetFromRebasedCurrentTime);
            }

            return target;
        }

        private bool WaitForNextAggregatorsCompletionCycle(TimeSpan waitPeriod)
        {
            ManualResetEvent aggregatorCompletionWaitEvent = _aggregatorCompletionWaitEvent;
            if (aggregatorCompletionWaitEvent == null)
            {
                return true;
            }

            try
            {
                return aggregatorCompletionWaitEvent.WaitOne(waitPeriod);
            }
            catch
            {
                return true;
            }
        }
    }
}
