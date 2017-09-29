using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal abstract class DataSeriesAggregatorBase : IMetricSeriesAggregator
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1310: C# Field must not contain an underscore", Justification = "By design: Structured name.")]
        private const int InternalExecutionState_Completed = -10000;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming Rules", "SA1310: C# Field must not contain an underscore", Justification = "By design: Structured name.")]
        private const int InternalExecutionState_Ready = 0;

        public const string AggregationIntervalMonikerPropertyKey = "_MS.AggregationIntervalMs";

        private readonly MetricSeries _dataSeries;
        private readonly MetricAggregationCycleKind _aggregationCycleKind;
        private readonly bool _isPersistent;
        
        private DateTimeOffset _periodStart;
        private DateTimeOffset _periodEnd;
        private IMetricValueFilter _valueFilter;
        private int _ongoingUpdates;

        public DataSeriesAggregatorBase(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            _dataSeries = dataSeries;
            _aggregationCycleKind = aggregationCycleKind;
            _isPersistent = configuration.RequiresPersistentAggregation;

            Reset(default(DateTimeOffset), default(IMetricValueFilter));
        }

        public MetricSeries DataSeries { get { return _dataSeries; } }

        public void Reset(DateTimeOffset periodStart, IMetricValueFilter valueFilter)
        {
            _periodStart = periodStart;
            _periodEnd = default(DateTimeOffset);
            _valueFilter = valueFilter;
            _ongoingUpdates = InternalExecutionState_Ready;
            ReinitializeAggregation();
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

        public bool TryRecycle()
        {
            if (_isPersistent)
            {
                return false;
            }

            Reset(default(DateTimeOffset), default(IMetricValueFilter));
            return true;
        }
        
        public void TrackValue(double metricValue)
        {
            if (_isPersistent)
            {
                // If we are persistent, just do the actual metric update without tracking ongoing updates:

                if (false == _valueFilter?.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }

                TrackFilteredValue(metricValue);
            }
            else
            {
                // If we are not persistent, keep track of ongoing updates before doing the actual update:

                if (_ongoingUpdates < InternalExecutionState_Ready)    // Soft check
                {
                    return;
                }

                if (false == _valueFilter?.WillConsume(_dataSeries, metricValue))
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
            if (_isPersistent)
            {
                // If we are persistent, just do the actual metric update without tracking ongoing updates:

                if (false == _valueFilter?.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }

                TrackFilteredValue(metricValue);
            }
            else
            {
                // If we are not persistent, keep track of ongoing updates before doing the actual update:

                if (_ongoingUpdates < InternalExecutionState_Ready)    // Soft check
                {
                    return;
                }

                if (false == _valueFilter?.WillConsume(_dataSeries, metricValue))
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

        protected abstract void ReinitializeAggregation();

        protected abstract void TrackFilteredValue(double metricValue);

        protected abstract void TrackFilteredValue(object metricValue);

        protected void StampVersionAndContextInfo(ITelemetry aggregate)
        {
            if (aggregate == null)
            {
                return;
            }

            if (DataSeries != null)
            {
                Util.CopyTelemetryContext(DataSeries.Context, aggregate.Context);
            }

            // TO-DO !!!
            //string sdkVersionPropertyValue = Microsoft.ApplicationInsights.Extensibility.Implementation.SdkVersionUtils.GetSdkVersion("m-agg:");
            //aggregate.Context.GetInternalContext().SdkVersion = sdkVersionPropertyValue;
            // ToDo !!!
        }

        protected void StampTimingInfo(ITelemetry aggregate, DateTimeOffset periodEnd)
        {
            if (aggregate == null)
            {
                return;
            }

            var metricAggregate = aggregate as MetricTelemetry;
            if (metricAggregate == null)
            {
                return;
            }

            TimeSpan period = periodEnd - _periodStart;
            long periodMillis = (long) period.TotalMilliseconds;

            IDictionary<string, string> props = metricAggregate.Properties;
            if (props != null)
            {
                props.Add(AggregationIntervalMonikerPropertyKey, periodMillis.ToString(CultureInfo.InvariantCulture));
            }

            metricAggregate.Timestamp = _periodStart;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Readability Rules",
            "SA1129: Do not use default value type constructor",
            Justification = "SpinWait used as designed")]
        private void EnsureUpdatesComplete(DateTimeOffset periodEnd)
        {
            // Start by decoubling from the series, so that we stop receiving more values:

            DataSeries.ClearAggregator(_aggregationCycleKind);

            int prevState = Interlocked.CompareExchange(ref _ongoingUpdates, InternalExecutionState_Completed, InternalExecutionState_Ready);

            // if (prevState == InternalExecutionState_Ready), the above suceeded and we now have _ongoingUpdates = InternalExecutionState_Completed.
            // otherwise, that assgnment can fail in 2 ways:
            //  a) prevState > InternalExecutionState_Ready:
            //     => we will go into the below loop trying to wait and to repeat the assignment
            //  b) prevState < InternalExecutionState_Ready:
            //     => It must already be that _ongoingUpdates = InternalExecutionState_Completed becasue of a cuncurrent assignment, so there is nothing to do.

            if (prevState > InternalExecutionState_Ready)
            {
                var spinWait = new SpinWait();
                prevState = Interlocked.CompareExchange(ref _ongoingUpdates, InternalExecutionState_Completed, InternalExecutionState_Ready);

                while (prevState > InternalExecutionState_Ready)
                {
                    spinWait.SpinOnce();

                    if (spinWait.Count % 100 == 0)
                    {
                        // Thread.Sleep(10);
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
    }
}