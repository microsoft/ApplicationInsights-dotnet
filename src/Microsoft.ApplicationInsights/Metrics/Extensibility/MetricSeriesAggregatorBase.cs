namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    /// <typeparam name="TBufferedValue">The actual type of the metric values. For most common metrics it's <c>double</c>.
    /// However, for example a metric collecting strings to dount the number of distinct entities might have <c>string</c>.</typeparam>
    public abstract class MetricSeriesAggregatorBase<TBufferedValue> : IMetricSeriesAggregator
    {
        private readonly MetricSeries _dataSeries;
        private readonly MetricAggregationCycleKind _aggregationCycleKind;
        private readonly bool _isPersistent;
        private readonly Func<MetricValuesBufferBase<TBufferedValue>> _metricValuesBufferFactory;

        private DateTimeOffset _periodStart;
        private IMetricValueFilter _valueFilter;

        private volatile MetricValuesBufferBase<TBufferedValue> _metricValuesBuffer;
        private volatile MetricValuesBufferBase<TBufferedValue> _metricValuesBufferRecycle = null;

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValuesBufferFactory">ToDo: Complete documentation before stable release.</param>
        /// <param name="configuration">ToDo: Complete documentation before stable release.</param>
        /// <param name="dataSeries">ToDo: Complete documentation before stable release.</param>
        /// <param name="aggregationCycleKind">ToDo: Complete documentation before stable release.</param>
        protected MetricSeriesAggregatorBase(
                                        Func<MetricValuesBufferBase<TBufferedValue>> metricValuesBufferFactory,
                                        IMetricSeriesConfiguration configuration,
                                        MetricSeries dataSeries,
                                        MetricAggregationCycleKind aggregationCycleKind)
        {
            Util.ValidateNotNull(metricValuesBufferFactory, nameof(metricValuesBufferFactory));
            Util.ValidateNotNull(configuration, nameof(configuration));

            this._dataSeries = dataSeries;
            this._aggregationCycleKind = aggregationCycleKind;
            this._isPersistent = configuration.RequiresPersistentAggregation;

            this._metricValuesBufferFactory = metricValuesBufferFactory;
            this._metricValuesBuffer = this.InvokeMetricValuesBufferFactory();

            this.Reset(default(DateTimeOffset), default(IMetricValueFilter));
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public MetricSeries DataSeries
        {
            get { return this._dataSeries; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodEnd">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public MetricAggregate CompleteAggregation(DateTimeOffset periodEnd)
        {
            if (!this._isPersistent)
            {
                this.DataSeries.ClearAggregator(this._aggregationCycleKind);
            }

            MetricAggregate aggregate = this.CreateAggregateUnsafe(periodEnd);
            return aggregate;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodStart">ToDo: Complete documentation before stable release.</param>
        public void Reset(DateTimeOffset periodStart)
        {
            this._periodStart = periodStart;

            this._metricValuesBuffer.ResetIndicesAndData();

            this.ResetAggregate();
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodStart">ToDo: Complete documentation before stable release.</param>
        /// <param name="valueFilter">ToDo: Complete documentation before stable release.</param>
        public void Reset(DateTimeOffset periodStart, IMetricValueFilter valueFilter)
        {
            this._valueFilter = valueFilter;
            this.Reset(periodStart);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValue">ToDo: Complete documentation before stable release.</param>
        public void TrackValue(double metricValue)
        {
            if (Double.IsNaN(metricValue))
            {
                return;
            }

            // Respect the filter. Note: Filter may be user code. If user code is broken, assume we accept the value.
            if (false == Util.FilterWillConsume(this._valueFilter, this._dataSeries, metricValue))
            {
                return;
            }

            // Prepare the metric value. If it is invalid, ConvertMetricValue may throw. This wil be propagated to the user.

            TBufferedValue value = this.ConvertMetricValue(metricValue);
            this.TrackFilteredConvertedValue(value);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValue">ToDo: Complete documentation before stable release.</param>
        public void TrackValue(object metricValue)
        {
            if (metricValue == null)
            {
                return;
            }

            // Respect the filter. Note: Filter may be user code. If user code is broken, assume we accept the value.
            if (false == Util.FilterWillConsume(this._valueFilter, this._dataSeries, metricValue))
            {
                return;
            }

            // Prepare the metric value. If it is invalid, ConvertMetricValue may throw. This wil be propagated to the user.
            TBufferedValue value = this.ConvertMetricValue(metricValue);
            this.TrackFilteredConvertedValue(value);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public bool TryRecycle()
        {
            if (this._isPersistent)
            {
                return false;
            }

            this.Reset(default(DateTimeOffset), default(IMetricValueFilter));
            return true;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodEnd">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public MetricAggregate CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            this.UpdateAggregate(this._metricValuesBuffer);

            return this.CreateAggregate(periodEnd);
        }

        #region Abstract Methods

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="periodEnd">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        protected abstract MetricAggregate CreateAggregate(DateTimeOffset periodEnd);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        protected abstract void ResetAggregate();

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValue">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        protected abstract TBufferedValue ConvertMetricValue(double metricValue);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="metricValue">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        protected abstract TBufferedValue ConvertMetricValue(object metricValue);

        /// <summary>
        /// Aggregators implement updating aggregate state from buffer by implemenmting this method (<c>UpdateAggregate_Stage1</c>)
        /// and its sibling method <c>UpdateAggregate_Stage2</c>. Stage 1 is the part of the update that needs to happen while holding
        /// a lock on the <c>metric values buffer</c> (e.g. extracting a summary from the buffer). Stage 2 is the part of the update
        /// that does not need such a lock.
        /// </summary>
        /// <param name="buffer">ToDo: Complete documentation before stable release.</param>
        /// <param name="minFlushIndex">ToDo: Complete documentation before stable release.</param>
        /// <param name="maxFlushIndex">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        protected abstract object UpdateAggregate_Stage1(MetricValuesBufferBase<TBufferedValue> buffer, int minFlushIndex, int maxFlushIndex);

        /// <summary>
        /// Aggregators implement updating aggregate state from buffer by implemenmting this method (<c>UpdateAggregate_Stage2</c>)
        /// and its sibling method <c>UpdateAggregate_Stage1</c>. Stage 1 is the part of the update that needs to happen while holding
        /// a lock on the <c>metric values buffer</c> (e.g. extracting a summary from the buffer). Stage 2 is the part of the update
        /// that does not need such a lock.
        /// </summary>
        /// <param name="stage1Result">ToDo: Complete documentation before stable release.</param>
        protected abstract void UpdateAggregate_Stage2(object stage1Result);

        #endregion Abstract Methods

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="aggregate">ToDo: Complete documentation before stable release.</param>
        /// <param name="periodEnd">ToDo: Complete documentation before stable release.</param>
        protected void AddInfo_Timing_Dimensions_Context(MetricAggregate aggregate, DateTimeOffset periodEnd)
        {
            if (aggregate == null)
            {
                return;
            }

            // Stamp Timing Info: 

            aggregate.AggregationPeriodStart = this._periodStart;
            aggregate.AggregationPeriodDuration = periodEnd - this._periodStart;

            if (this.DataSeries != null)
            {
                // Stamp dimensions:

                if (this.DataSeries.DimensionNamesAndValues != null)
                {
                    foreach (KeyValuePair<string, string> dimNameVal in this.DataSeries.DimensionNamesAndValues)
                    {
                        aggregate.Dimensions[dimNameVal.Key] = dimNameVal.Value;
                    }
                }
            }
        }

#if DEBUG
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
#pragma warning disable SA1201 // Elements must appear in the correct order
                              /// <summary>For debug purposes. Not compiled into release build.</summary>
        public static volatile int s_countBufferWaitSpinEvents = 0;

        /// <summary>For debug purposes. Not compiled into release build.</summary>
        public static volatile int s_countBufferWaitSpinCycles = 0;

        /// <summary>For debug purposes. Not compiled into release build.</summary>
        public static volatile int s_timeBufferWaitSpinMillis = 0;

        /// <summary>For debug purposes. Not compiled into release build.</summary>
        public static volatile int s_countBufferFlushes = 0;

        /// <summary>For debug purposes. Not compiled into release build.</summary>
        public static volatile int s_countNewBufferObjectsCreated = 0;
#pragma warning restore SA1201 // Elements must appear in the correct order
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
#endif

        /// <summary>
        /// This method is the meat of the lock-free aggregation logic.
        /// </summary>
        /// <param name="metricValue">Already filtered and conveted value to be tracked.
        ///     We know that the value is not Double.NaN and not null and it passed trought any filters.</param>
        private void TrackFilteredConvertedValue(TBufferedValue metricValue)
        {
            // Get reference to the current buffer:
            MetricValuesBufferBase<TBufferedValue> buffer = this._metricValuesBuffer;

            // Get the index at which to store metricValue into the buffer:
            int index = buffer.IncWriteIndex();

            // Check to see whether we are past the end of the buffer. 
            // If we are, it means that some *other* thread hit exactly the end (wrote the last value that fits into the buffer) and is currently flushing.
            // If we are, we will spin and wait.
            if (index >= buffer.Capacity)
            {
#if DEBUG
                int startMillis = Environment.TickCount;
#endif
#pragma warning disable SA1129 // Do not use default value type constructor
                var spinWait = new SpinWait();
#pragma warning restore SA1129 // Do not use default value type constructor

                // It could be that the thread that was flushing is done and has updated the buffer pointer.
                // We refresh our local reference and see if we now have a valid index into the buffer.
                buffer = this._metricValuesBuffer;
                index = buffer.IncWriteIndex();

                while (index >= buffer.Capacity)
                {
                    // Still not valid index into the buffer. Spin and try again.
                    spinWait.SpinOnce();
#if DEBUG
                    unchecked
                    {
                        Interlocked.Increment(ref s_countBufferWaitSpinCycles);
                    }
#endif
                    if (spinWait.Count % 100 == 0)
                    {
                        // In tests (including stress tests) we always finished wating before 100 cycles.
                        // However, this is a protection against en extreme case on a slow machine.
                        // We will back off and sleep for a few millisecs to give the machine a chance to finish current tasks.

                        Task.Delay(10).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                    }

                    // Check to see whether the thread that was flushing is done and has updated the buffer pointer.
                    // We refresh our local reference and see if we now have a valid index into the buffer.
                    buffer = this._metricValuesBuffer;
                    index = buffer.IncWriteIndex();
                }
#if DEBUG
                unchecked
                {
                    int periodMillis = Environment.TickCount - startMillis;
                    int currentSpinMillis = s_timeBufferWaitSpinMillis;
                    int prevSpinMillis = Interlocked.CompareExchange(ref s_timeBufferWaitSpinMillis, currentSpinMillis + periodMillis, currentSpinMillis);
                    while (prevSpinMillis != currentSpinMillis)
                    {
                        currentSpinMillis = s_timeBufferWaitSpinMillis;
                        prevSpinMillis = Interlocked.CompareExchange(ref s_timeBufferWaitSpinMillis, currentSpinMillis + periodMillis, currentSpinMillis);
                    }

                    Interlocked.Increment(ref s_countBufferWaitSpinEvents);
                }
#endif
            }

            // Ok, so now we know that (0 <= index = buffer.Capacity). Write the value to the buffer:
            buffer.WriteValue(index, metricValue);

            // If this was the last value that fits into the buffer, we must flush the buffer:
            if (index == buffer.Capacity - 1)
            {
                // Before we begin flushing (which is can take time), we update the _metricValuesBuffer to a fresh buffer that is ready to take values.
                // That way threads do notneed to spin and wait until we flush and can begin writing values.

                // We try to recycle a previous buffer to lower stress on GC and to lower Gen-2 heap fragmentation.
                // The lifetime of an buffer can easily be a minute or so and then it can get into Gen-2 GC heap.
                // If we then, keep throwing such buffers away we can fragment the Gen-2 heap. To avoid this we employ
                // a simple form of best-effort object pooling.

                // Get buffer from pool and reset the pool:
                MetricValuesBufferBase<TBufferedValue> newBufer = Interlocked.Exchange(ref this._metricValuesBufferRecycle, null);
                
                if (newBufer != null)
                {
                    // If we were succesful in getting a recycled buffer from the pool, we will try to use it as the new buffer.
                    // If we successfully the the recycled buffer to be the new buffer, we will reset it to prepare for data.
                    // Otherwise we will just throw it away.

                    MetricValuesBufferBase<TBufferedValue> prevBuffer = Interlocked.CompareExchange(ref this._metricValuesBuffer, newBufer, buffer);
                    if (prevBuffer == buffer)
                    {
                        newBufer.ResetIndices();
                    }
                }
                else
                {
                    // If we were succesful in getting a recycled buffer from the pool, we will create a new one.

                    newBufer = this.InvokeMetricValuesBufferFactory();
                    Interlocked.CompareExchange(ref this._metricValuesBuffer, newBufer, buffer);
                }

                // Ok, now we have either set a new buffer that is ready to be used, or we have determined using CompareExchange
                // that another thread set a new buffer and we do not need to do it here.

                // Now we can actually flush the buffer:

                this.UpdateAggregate(buffer);

                // The buffer is now flushed. If the slot for the best-effor object pooling is free, use it:
                Interlocked.CompareExchange(ref this._metricValuesBufferRecycle, buffer, null);
            }
        }

        /// <summary>
        /// Flushes the values buffer to update the aggregate state held by subclasses.
        /// </summary>
        /// <param name="buffer">ToDo: Complete documentation before stable release.</param>
        private void UpdateAggregate(MetricValuesBufferBase<TBufferedValue> buffer)
        {
            if (buffer == null)
            {
                return;
            }

#if DEBUG
            unchecked
            {
                Interlocked.Increment(ref s_countBufferFlushes);
            }
#endif

            object stage1Result;

            // This lock is only contended if a user called CreateAggregateUnsafe or CompleteAggregation.
            // This is very unlikely to be the case in a tight loop.
            lock (buffer)
            {
                int maxFlushIndex = Math.Min(buffer.PeekLastWriteIndex(), buffer.Capacity - 1);
                int minFlushIndex = buffer.NextFlushIndex;

                if (minFlushIndex > maxFlushIndex)
                {
                    return;
                }

                stage1Result = this.UpdateAggregate_Stage1(buffer, minFlushIndex, maxFlushIndex);
                
                buffer.NextFlushIndex = maxFlushIndex + 1;
            }

            this.UpdateAggregate_Stage2(stage1Result);
        }

        private MetricValuesBufferBase<TBufferedValue> InvokeMetricValuesBufferFactory()
        {
#if DEBUG
            unchecked
            {
                Interlocked.Increment(ref s_countNewBufferObjectsCreated);
            }
#endif
            MetricValuesBufferBase<TBufferedValue> buffer = this._metricValuesBufferFactory();

            if (buffer == null)
            {
                throw new InvalidOperationException($"{nameof(this._metricValuesBufferFactory)}-delegate returned null. This is not allowed. Bad aggregator?");
            }

            return buffer;
        }
    }
}
