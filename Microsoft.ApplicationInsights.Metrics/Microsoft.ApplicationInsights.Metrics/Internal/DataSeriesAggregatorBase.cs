using System;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal abstract class DataSeriesAggregatorBase : IMetricDataSeriesAggregator
    {
        private const int InternalExecutionState_Completed = -10000;
        private const int InternalExecutionState_Ready = 0;

        private readonly IMetricConfiguration _configuration;
        private readonly MetricDataSeries _dataSeries;
        private readonly MetricConsumerKind _consumerKind;
        private readonly bool _isPersistent;

        private IMetricValueFilter _valueFilter;
        private DateTimeOffset _periodStart;
        private DateTimeOffset _periodEnd;
        private int _ongoingUpdates;

        public DataSeriesAggregatorBase(IMetricConfiguration configuration, MetricDataSeries dataSeries, MetricConsumerKind consumerKind)
        {
            _configuration = configuration;
            _dataSeries = dataSeries;
            _consumerKind = consumerKind;
            _isPersistent = configuration.RequiresPersistentAggregation;

            _valueFilter = null;
            _periodStart = default(DateTimeOffset);
            _periodEnd = default(DateTimeOffset);
            _ongoingUpdates = InternalExecutionState_Ready;
        }

        public DateTimeOffset PeriodStart { get { return _periodStart; } }

        public DateTimeOffset PeriodEnd { get { return _periodEnd; } }

        public MetricDataSeries DataSeries { get { return _dataSeries; } }

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

        public void Initialize(DateTimeOffset periodStart, IMetricValueFilter valueFilter)
        {
            _periodStart = periodStart;
            _valueFilter = valueFilter;
        }

        public virtual ITelemetry CompleteAggregation(DateTimeOffset periodEnd)
        {
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
            return aggregate;
        }

        private void EnsureUpdatesComplete(DateTimeOffset periodEnd)
        {
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
                        Thread.Sleep(10);
                    }

                    prevState = Interlocked.CompareExchange(ref _ongoingUpdates, InternalExecutionState_Completed, InternalExecutionState_Ready);
                }
            }

            if (prevState == InternalExecutionState_Ready)
            {
                _periodEnd = periodEnd;
                DataSeries.ClearAggregator(_consumerKind);
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
                if (!_valueFilter.WillConsume(_dataSeries, metricValue))
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
                if (!_valueFilter.WillConsume(_dataSeries, metricValue))
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

        public abstract ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd);

        protected abstract void TrackFilteredValue(uint metricValue);

        protected abstract void TrackFilteredValue(double metricValue);

        protected abstract void TrackFilteredValue(object metricValue);

        
    }
}