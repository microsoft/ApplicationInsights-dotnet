using System;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal abstract class DataSeriesAggregatorBase : IMetricSeriesAggregator
    {
        private const int InternalExecutionState_Completed = -10000;
        private const int InternalExecutionState_Ready = 0;

        private readonly MetricSeries _dataSeries;
        private readonly MetricConsumerKind _consumerKind;
        private readonly bool _isPersistent;
        
        private DateTimeOffset _periodStart;
        private DateTimeOffset _periodEnd;
        private IMetricValueFilter _valueFilter;
        private int _ongoingUpdates;

        private ITelemetry _completedAggregate;

        public DataSeriesAggregatorBase(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricConsumerKind consumerKind)
        {
            _dataSeries = dataSeries;
            _consumerKind = consumerKind;
            _isPersistent = configuration.RequiresPersistentAggregation;

            Initialize(default(DateTimeOffset), default(IMetricValueFilter));
        }

        public DateTimeOffset PeriodStart { get { return _periodStart; } }

        public DateTimeOffset PeriodEnd { get { return _periodEnd; } }

        public MetricSeries DataSeries { get { return _dataSeries; } }

        public bool IsCompleted {
            get
            {
                if (_isPersistent)
                {
                    return false;
                }

                int internalUpdateState = _ongoingUpdates;

                if (internalUpdateState == InternalExecutionState_Completed)
                {
                    return true;
                }

                if (internalUpdateState >= InternalExecutionState_Ready)
                {
                    return false;
                }

                throw new InvalidOperationException($"Unexpected value of {nameof(_ongoingUpdates)}: {internalUpdateState}.");
            }
        }

        public virtual bool SupportsRecycle { get { return (! _isPersistent); } }

        public void Initialize(DateTimeOffset periodStart, IMetricValueFilter valueFilter)
        {
            _periodStart = periodStart;
            _periodEnd = default(DateTimeOffset);
            _valueFilter = valueFilter;
            _ongoingUpdates = InternalExecutionState_Ready;

            _completedAggregate = null;
        }

        public virtual ITelemetry CompleteAggregation(DateTimeOffset periodEnd)
        {
            ITelemetry completedAggregate = _completedAggregate;
            if (completedAggregate != null)
            {
                return completedAggregate;
            }

            // Aggregators may transiently have inconsistent state in order to avoid locking.
            // We wait until ongoing updates are complete and then prevent the aggregator from further updating.
            // However, we do NOT do this for persistent aggregators, so they may transinetly inconsistent aggregates.
            // However this is benign. For example, a persistent counter may have sum and count mismatching because the one was updated and the other not yet.
            // But each atomic internal value should be valid. So derived statistics (like Average) may be transiently incorrect, however:
            //  - In the context of large numbers errors tend to be insignificant
            //  - The small differences in potential errors at different time periods make them look like minimal noise rather than a significant error.
            if (! _isPersistent)
            {
                EnsureUpdatesComplete(periodEnd);
            }

            ITelemetry aggregate = CreateAggregateUnsafe(periodEnd);
            ITelemetry prevCompletedAggregate = Interlocked.CompareExchange(ref _completedAggregate, aggregate, null);
            return (prevCompletedAggregate ?? aggregate);
        }

        public bool TryRecycle()
        {
            if (_isPersistent)
            {
                return false;
            }

            ITelemetry prevCompletedAgregate = Interlocked.Exchange(ref _completedAggregate, null);
            if (prevCompletedAgregate == null)
            {
                return false;
            }

            Initialize(default(DateTimeOffset), default(IMetricValueFilter));
            return RecycleUnsafe();
        }

        private void EnsureUpdatesComplete(DateTimeOffset periodEnd)
        {
            DataSeries.ClearAggregator(_consumerKind);

            int prevState = Interlocked.CompareExchange(ref _ongoingUpdates, InternalExecutionState_Completed, InternalExecutionState_Ready);

            if (prevState > InternalExecutionState_Ready)
            {
                var spinWait = new SpinWait();
                prevState = Interlocked.CompareExchange(ref _ongoingUpdates, InternalExecutionState_Completed, InternalExecutionState_Ready);

                while (prevState > InternalExecutionState_Ready)
                {
                    spinWait.SpinOnce();

                    if (spinWait.Count % 100 == 0)
                    {
                        //Thread.Sleep(10);
                        Task.Delay(10).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                    }

                    prevState = Interlocked.CompareExchange(ref _ongoingUpdates, InternalExecutionState_Completed, InternalExecutionState_Ready);
                }
            }

            if (prevState == InternalExecutionState_Ready)
            {
                _periodEnd = periodEnd;
            }
        }

        public void TrackValue(uint metricValue)
        {
            if (_valueFilter != null)
            {
                if (! _valueFilter.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }
            }
            
            if (_isPersistent)
            {
                // If we are persistent, just do the actual metric update without tracking ongoing updates:
                TrackFilteredValue(metricValue);
            }
            else
            {
                // If we are not persistent, keep track of ongoing updates before doing the actual update:

                if (_ongoingUpdates < InternalExecutionState_Ready)    // Soft check
                {
                    return;
                }

                int internalUpdateState = Interlocked.Increment(ref _ongoingUpdates);
                try
                {
                    // Hard check for being completed:
                    if (internalUpdateState < InternalExecutionState_Ready)
                    {
                        return; // This will decrement _ongoingUpdates in the finally block.
                    }

                    // Do the actual tracking:
                    TrackFilteredValue(metricValue);
                }
                finally
                {
                    Interlocked.Decrement(ref _ongoingUpdates);
                }
            }
        }

        public void TrackValue(double metricValue)
        {
            if (_valueFilter != null)
            {
                if (! _valueFilter.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }
            }

            if (_isPersistent)
            {
                // If we are persistent, just do the actual metric update without tracking ongoing updates:
                TrackFilteredValue(metricValue);
            }
            else
            {
                // If we are not persistent, keep track of ongoing updates before doing the actual update:

                if (_ongoingUpdates < InternalExecutionState_Ready)    // Soft check
                {
                    return;
                }

                int internalUpdateState = Interlocked.Increment(ref _ongoingUpdates);
                try
                {
                    // Hard check for being completed:
                    if (internalUpdateState < InternalExecutionState_Ready)
                    {
                        return; // This will decrement _ongoingUpdates in the finally block.
                    }

                    // Do the actual tracking:
                    TrackFilteredValue(metricValue);
                }
                finally
                {
                    Interlocked.Decrement(ref _ongoingUpdates);
                }
            }
        }

        public void TrackValue(object metricValue)
        {
            if (_valueFilter != null)
            {
                if (! _valueFilter.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }
            }

            if (_isPersistent)
            {
                // If we are persistent, just do the actual metric update without tracking ongoing updates:
                TrackFilteredValue(metricValue);
            }
            else
            {
                // If we are not persistent, keep track of ongoing updates before doing the actual update:

                if (_ongoingUpdates < InternalExecutionState_Ready)    // Soft check
                {
                    return;
                }

                int internalUpdateState = Interlocked.Increment(ref _ongoingUpdates);
                try
                {
                    // Hard check for being completed:
                    if (internalUpdateState < InternalExecutionState_Ready)
                    {
                        return; // This will decrement _ongoingUpdates in the finally block.
                    }

                    // Do the actual tracking:
                    TrackFilteredValue(metricValue);
                }
                finally
                {
                    Interlocked.Decrement(ref _ongoingUpdates);
                }
            }
        }

        protected abstract bool RecycleUnsafe();

        public abstract ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd);

        protected abstract void TrackFilteredValue(uint metricValue);

        protected abstract void TrackFilteredValue(double metricValue);

        protected abstract void TrackFilteredValue(object metricValue);

        
    }
}