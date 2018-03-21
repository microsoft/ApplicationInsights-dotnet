namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    /// <typeparam name="TValue">The tyoe of values held in the buffer.</typeparam>
    public abstract class MetricValuesBufferBase<TValue>
    {
        private readonly TValue[] _values;

        private int _lastWriteIndex = -1;
        private volatile int _nextFlushIndex = 0;

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="capacity">ToDo: Complete documentation before stable release.</param>
        public MetricValuesBufferBase(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _values = new TValue[capacity];
            ResetValues(_values);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values.Length; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int NextFlushIndex
        {
            get { return _nextFlushIndex; }
            set { _nextFlushIndex = value; }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncWriteIndex()
        {
            return Interlocked.Increment(ref _lastWriteIndex);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekLastWriteIndex()
        {
            return Volatile.Read(ref _lastWriteIndex);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValue(int index, TValue value)
        {
            WriteValueOnce(_values, index, value);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public void ResetIndices()
        {
            _nextFlushIndex = 0;
            Interlocked.Exchange(ref _lastWriteIndex, -1);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public void ResetIndicesAndData()
        {
            Interlocked.Exchange(ref _lastWriteIndex, Capacity);
            ResetValues(_values);
            ResetIndices();
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        public TValue GetAndResetValue(int index)
        {
            TValue value = GetAndResetValueOnce(_values, index);

            if (IsInvalidValue(value))
            {
#pragma warning disable SA1129 // Do not use default value type constructor
                var spinWait = new SpinWait();
#pragma warning restore SA1129 // Do not use default value type constructor

                value = GetAndResetValueOnce(_values, index);
                while (IsInvalidValue(value))
                {
                    spinWait.SpinOnce();
                    
                    if (spinWait.Count % 100 == 0)
                    {
                        // In tests (including stress tests) we always finished wating before 100 cycles.
                        // However, this is a protection against en extreme case on a slow machine. 
                        Task.Delay(10).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                    }

                    value = GetAndResetValueOnce(_values, index);
                }
            }

            return value;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">.</param>
        protected abstract void ResetValues(TValue[] values);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TValue GetAndResetValueOnce(TValue[] values, int index);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void WriteValueOnce(TValue[] values, int index, TValue value);

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool IsInvalidValue(TValue value);
    }

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public sealed class MetricValuesBuffer_Double : MetricValuesBufferBase<double>
    {
        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="capacity">ToDo: Complete documentation before stable release.</param>
        public MetricValuesBuffer_Double(int capacity)
            : base(capacity)
        {
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(double value)
        {
            return Double.IsNaN(value);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        protected override void ResetValues(double[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], Double.NaN))
            {
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double GetAndResetValueOnce(double[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], Double.NaN);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(double[] values, int index, double value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    public sealed class MetricValuesBuffer_Object : MetricValuesBufferBase<object>
    {
        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="capacity">ToDo: Complete documentation before stable release.</param>
        public MetricValuesBuffer_Object(int capacity)
            : base(capacity)
        {
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(object value)
        {
            return (value == null);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        protected override void ResetValues(object[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], null))
            {
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object GetAndResetValueOnce(object[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], null);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="values">ToDo: Complete documentation before stable release.</param>
        /// <param name="index">ToDo: Complete documentation before stable release.</param>
        /// <param name="value">ToDo: Complete documentation before stable release.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(object[] values, int index, object value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
#pragma warning restore SA1649 // File name must match first type name
}
