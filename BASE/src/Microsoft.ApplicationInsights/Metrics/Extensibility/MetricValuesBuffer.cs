namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>The metric values buffer is the the heart of the lock-free (well, mostly) aggregation logic.
    /// If allows to quickly collect metric values and to update the running aggregate at regular intervals.
    /// This is required becasue aggregates for some aggregation kinds are expensive to update (e.g. some percentile
    /// algorithms) and/or require a lock. By collecting a bunch of values first, the expensive/locked operation can
    /// occur less frequently.</summary>
    /// <typeparam name="TValue">The type of values held in the buffer.</typeparam>
    /// @PublicExposureCandidate
    internal abstract class MetricValuesBufferBase<TValue>
    {
        private readonly TValue[] values;

        private int lastWriteIndex = -1;
        private volatile int nextFlushIndex = 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214: Do not call overridable methods in constructors", Justification = "Call chain has been reviewed.")]
        public MetricValuesBufferBase(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.values = new TValue[capacity];
            this.ResetValues(this.values);
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this.values.Length; }
        }

        public int NextFlushIndex
        {
            get { return this.nextFlushIndex; }
            set { this.nextFlushIndex = value; }
        }

        protected abstract TValue DefaultValue { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncWriteIndex()
        {
            return Interlocked.Increment(ref this.lastWriteIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekLastWriteIndex()
        {
            return Volatile.Read(ref this.lastWriteIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValue(int index, TValue value)
        {
            this.WriteValueOnce(this.values, index, value);
        }

        public void ResetIndices()
        {
            this.nextFlushIndex = 0;
            Interlocked.Exchange(ref this.lastWriteIndex, -1);
        }

        public void ResetIndicesAndData()
        {
            Interlocked.Exchange(ref this.lastWriteIndex, this.Capacity);
            this.ResetValues(this.values);
            this.ResetIndices();
        }

        public TValue GetAndResetValue(int index)
        {
            for (int spinCountdown = 10000; spinCountdown > 0; spinCountdown--)
            {
                TValue value = this.GetAndResetValueOnce(this.values, index);

                if (this.IsInvalidValue(value))
                {
                    if (spinCountdown % 100 == 0)
                    {
                        // In tests (including stress tests) we always finished waiting before 100 cycles.
                        // However, this is a protection against en extreme case on a slow machine. 
                        Task.Delay(10).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                    }

                    continue;
                }
                else
                {
                    return value;
                }
            }

            // exceeded maximum spin count
            return this.DefaultValue;
        }

        protected abstract void ResetValues(TValue[] values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TValue GetAndResetValueOnce(TValue[] values, int index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void WriteValueOnce(TValue[] values, int index, TValue value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool IsInvalidValue(TValue value);
    }

    /// <summary><seealso cref="MetricValuesBufferBase{TValue}"/></summary>
    /// @PublicExposureCandidate
    internal sealed class MetricValuesBuffer_Double : MetricValuesBufferBase<double>
    {
        public MetricValuesBuffer_Double(int capacity)
            : base(capacity)
        {
        }

        protected override double DefaultValue => double.NaN;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(double value)
        {
            return Double.IsNaN(value);
        }

        protected override void ResetValues(double[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], Double.NaN))
            {
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double GetAndResetValueOnce(double[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], Double.NaN);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(double[] values, int index, double value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }

    /// <summary><seealso cref="MetricValuesBufferBase{TValue}"/></summary>
    /// @PublicExposureCandidate
    internal sealed class MetricValuesBuffer_Object : MetricValuesBufferBase<object>
    {
        public MetricValuesBuffer_Object(int capacity)
            : base(capacity)
        {
        }

        protected override object DefaultValue => null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(object value)
        {
            return value == null;
        }

        protected override void ResetValues(object[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], null))
            {
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object GetAndResetValueOnce(object[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(object[] values, int index, object value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
#pragma warning restore SA1649 // File name must match first type name
}
