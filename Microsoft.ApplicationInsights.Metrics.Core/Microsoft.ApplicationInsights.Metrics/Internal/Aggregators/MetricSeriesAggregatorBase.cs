using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal abstract class MetricSeriesAggregatorBase<TMetricValue> : IMetricSeriesAggregator
    {

        private readonly MetricSeries _dataSeries;
        private readonly MetricAggregationCycleKind _aggregationCycleKind;
        private readonly bool _isPersistent;

        private DateTimeOffset _periodStart;
        private IMetricValueFilter _valueFilter;

        private MetricValuesBuffer<TMetricValue> _metricValuesBuffer = null;
        private MetricValuesBuffer<TMetricValue> _metricValuesBufferRecycle = null;

        public MetricSeriesAggregatorBase(IMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricAggregationCycleKind aggregationCycleKind)
        {
            Util.ValidateNotNull(configuration, nameof(configuration));

            _dataSeries = dataSeries;
            _aggregationCycleKind = aggregationCycleKind;
            _isPersistent = configuration.RequiresPersistentAggregation;

            Reset(default(DateTimeOffset), default(IMetricValueFilter));
        }

        public MetricSeries DataSeries { get { return _dataSeries; } }

        public MetricAggregate CompleteAggregation(DateTimeOffset periodEnd)
        {
            if (!_isPersistent)
            {
                DataSeries.ClearAggregator(_aggregationCycleKind);
            }

            MetricAggregate aggregate = CreateAggregateUnsafe(periodEnd);
            return aggregate;
        }

        public void Reset(DateTimeOffset periodStart)
        {
            _periodStart = periodStart;

            MetricValuesBuffer<TMetricValue> prevBuffer = Interlocked.Exchange(ref _metricValuesBuffer, null);
            Interlocked.CompareExchange(ref _metricValuesBufferRecycle, prevBuffer, null);

            ResetAggregate();
        }

        public void Reset(DateTimeOffset periodStart, IMetricValueFilter valueFilter)
        {
            _valueFilter = valueFilter;
            Reset(periodStart);
        }

        public void TrackValue(double metricValue)
        {
            try     // Respect the filter. Note: Filter may be user code. If user code is broken, assume we accept the value.
            {
                if (false == _valueFilter?.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }
            }
            catch { }

            // Prepare the metric value. If it is invalid, ConvertMetricValue may throw. This wil be propagated to the user.
            TMetricValue value = ConvertMetricValue(metricValue);
            TrackFilteredConvertedValue(value);
        }



        public void TrackValue(object metricValue)
        {
            try     // Respect the filter. Note: Filter may be user code. If user code is broken, assume we accept the value.
            {
                if (false == _valueFilter?.WillConsume(_dataSeries, metricValue))
                {
                    return;
                }
            }
            catch { }

            // Prepare the metric value. If it is invalid, ConvertMetricValue may throw. This wil be propagated to the user.
            TMetricValue value = ConvertMetricValue(metricValue);
            TrackFilteredConvertedValue(value);
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

        public MetricAggregate CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            SnapAndFlushBuffer();
            return CreateAggregate(periodEnd);
        }

        #region Abstract Methods
        protected abstract MetricAggregate CreateAggregate(DateTimeOffset periodEnd);

        protected abstract void ResetAggregate();

        protected abstract TMetricValue ConvertMetricValue(double metricValue);

        protected abstract TMetricValue ConvertMetricValue(object metricValue);

        protected abstract void UpdateAggregate(MetricValuesBuffer<TMetricValue> buffer);
        #endregion Abstract Methods

        protected void AddInfo_Timing_Dimensions_Context(MetricAggregate aggregate, DateTimeOffset periodEnd)
        {
            if (aggregate == null)
            {
                return;
            }

            // Stamp Timing Info: 

            aggregate.AggregationPeriodStart = _periodStart;
            aggregate.AggregationPeriodDuration = periodEnd - _periodStart;

            if (DataSeries != null)
            {
                // Stamp dimensions:

                if (DataSeries.DimensionNamesAndValues != null)
                {
                    foreach (KeyValuePair<string, string> dimNameVal in DataSeries.DimensionNamesAndValues)
                    {
                        aggregate.Dimensions[dimNameVal.Key] = dimNameVal.Value;
                    }
                }
            }
        }

        private void TrackFilteredConvertedValue(TMetricValue metricValue)
        {
            MetricValuesBuffer<TMetricValue> buffer = GetOrCreateBuffer();
            bool canAdd = buffer.TryAdd(metricValue);

            while (! canAdd)
            {
                FlushBuffer(buffer);

                buffer = GetOrCreateBuffer();
                canAdd = buffer.TryAdd(metricValue);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MetricValuesBuffer<TMetricValue> GetOrCreateBuffer()
        {
            MetricValuesBuffer<TMetricValue> existingBuffer = _metricValuesBuffer;

            if (existingBuffer != null)
            {
                return existingBuffer;
            }

            return GetOrCreateNewBuffer();
        }

        private MetricValuesBuffer<TMetricValue> GetOrCreateNewBuffer()
        {
            // A buffer is a relatively small object that can easily live for a minute or so. So, it can get into the Gen-2 GC heap, and become
            // garbage soon thereafter. This can lead to a fragmentation of the Gen-2 heap. To mitigate, we employ a simple form of best-effort object pooling.
            MetricValuesBuffer<TMetricValue> newBuffer = Interlocked.Exchange(ref _metricValuesBufferRecycle, null);
            if (newBuffer == null)
            {
                newBuffer = new MetricValuesBuffer<TMetricValue>();
            }
            else
            {
                newBuffer.Reset();
            }

            MetricValuesBuffer<TMetricValue> prevBuffer = Interlocked.CompareExchange(ref _metricValuesBuffer, newBuffer, null);
            MetricValuesBuffer<TMetricValue> usefulBuffer = prevBuffer ?? newBuffer;

            return usefulBuffer;
        }
        
        private void SnapAndFlushBuffer()
        {
            MetricValuesBuffer<TMetricValue> snappedBufer = Interlocked.Exchange(ref _metricValuesBuffer, null);
            FlushBuffer(snappedBufer);
        }

        private void SnapAndFlushBuffer(MetricValuesBuffer<TMetricValue> bufferToSnap)
        {
            MetricValuesBuffer<TMetricValue> prevBufer = Interlocked.CompareExchange(ref _metricValuesBuffer, null, bufferToSnap);

            if (prevBufer != bufferToSnap)
            {
                // This means we lost a race for flushing the buffer and the current buffer is not the one we wanted to flush.
                // In this case, there is nothing for us to do.
                return;
            }

            // Ok, we won the race to flush. The current buffer was 'bufferToSnap' and we are the only one thread that changed that to null.
            // Now we can update the running aggregate. Every aggergator implementation can do this in its own way, and potentially under a lock.
            FlushBuffer(bufferToSnap);
        }

        private void FlushBuffer(MetricValuesBuffer<TMetricValue> buffer)
        {
            UpdateAggregate(buffer);

            // A buffer is a relatively small object that can easily live for a minute or so. So, it can get into the Gen-2 GC heap, and become
            // garbage soon thereafter. This can lead to a fragmentation of the Gen-2 heap. To mitigate, we employ a simple form of best-effort object pooling.
            Interlocked.CompareExchange(ref _metricValuesBufferRecycle, buffer, null);

        }
    }
}
