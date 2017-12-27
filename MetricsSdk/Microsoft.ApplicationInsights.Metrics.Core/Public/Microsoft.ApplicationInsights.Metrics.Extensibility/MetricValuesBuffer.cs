using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

    /// <summary />
    /// <typeparam name="TValue"></typeparam>
    public abstract class MetricValuesBufferBase<TValue>
    {
        private readonly TValue[] _values;

        private int _lastWriteIndex = -1;
        private volatile int _nextFlushIndex = 0;

        /// <summary>
        /// </summary>
        /// <param name="capacity"></param>
        public MetricValuesBufferBase(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _values = new TValue[capacity];
            ResetValues(_values);
        }

        /// <summary>
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values.Length; }
        }

        /// <summary>
        /// </summary>
        public int NextFlushIndex
        {
            get { return _nextFlushIndex; }
            set { _nextFlushIndex = value; }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncWriteIndex()
        {
            return Interlocked.Increment(ref _lastWriteIndex);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekLastWriteIndex()
        {
            return Volatile.Read(ref _lastWriteIndex);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValue(int index, TValue value)
        {
            WriteValueOnce(_values, index, value);
        }

        /// <summary />
        public void ResetIndices()
        {
            _nextFlushIndex = 0;
            Interlocked.Exchange(ref _lastWriteIndex, -1);
        }

        /// <summary />
        public void ResetIndicesAndData()
        {
            Interlocked.Exchange(ref _lastWriteIndex, Capacity);
            ResetValues(_values);
            ResetIndices();
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
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

        /// <summary />
        /// <param name="values">.</param>
        protected abstract void ResetValues(TValue[] values);

        /// <summary />
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TValue GetAndResetValueOnce(TValue[] values, int index);

        /// <summary />
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void WriteValueOnce(TValue[] values, int index, TValue value);

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool IsInvalidValue(TValue value);
    }

    /// <summary>
    /// </summary>
    public sealed class MetricValuesBuffer_Double : MetricValuesBufferBase<double>
    {
        /// <summary>
        /// </summary>
        /// <param name="capacity"></param>
        public MetricValuesBuffer_Double(int capacity)
            : base(capacity)
        {
        }

        /// <summary />
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(double value)
        {
            return Double.IsNaN(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        protected override void ResetValues(double[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], Double.NaN))
            {
                ;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double GetAndResetValueOnce(double[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], Double.NaN);
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(double[] values, int index, double value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }

    /// <summary />
    public sealed class MetricValuesBuffer_Object : MetricValuesBufferBase<object>
    {
        /// <summary>
        /// </summary>
        /// <param name="capacity"></param>
        public MetricValuesBuffer_Object(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(object value)
        {
            return (value == null);
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        protected override void ResetValues(object[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], null))
            {
                ;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object GetAndResetValueOnce(object[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], null);
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(object[] values, int index, object value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
#pragma warning restore SA1649 // File name must match first type name
}
