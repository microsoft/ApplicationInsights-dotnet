namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>@ToDo: Complete documentation before stable release. {821}</summary>
    /// <typeparam name="TValue">The tyoe of values held in the buffer.</typeparam>
    /// @PublicExposureCandidate
    internal abstract class MetricValuesBufferBase<TValue>
    {
        private readonly TValue[] values;

        private int lastWriteIndex = -1;
        private volatile int nextFlushIndex = 0;

        /// <summary>@ToDo: Complete documentation before stable release. {327}</summary>
        /// <param name="capacity">@ToDo: Complete documentation before stable release. {055}</param>
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

        /// <summary>Gets @ToDo: Complete documentation before stable release. {982}</summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this.values.Length; }
        }

        /// <summary>Gets or sets @ToDo: Complete documentation before stable release. {934}</summary>
        public int NextFlushIndex
        {
            get { return this.nextFlushIndex; }
            set { this.nextFlushIndex = value; }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {997}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {390}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncWriteIndex()
        {
            return Interlocked.Increment(ref this.lastWriteIndex);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {243}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {695}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekLastWriteIndex()
        {
            return Volatile.Read(ref this.lastWriteIndex);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {205}</summary>
        /// <param name="index">@ToDo: Complete documentation before stable release. {545}</param>
        /// <param name="value">@ToDo: Complete documentation before stable release. {562}</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValue(int index, TValue value)
        {
            this.WriteValueOnce(this.values, index, value);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {182}</summary>
        public void ResetIndices()
        {
            this.nextFlushIndex = 0;
            Interlocked.Exchange(ref this.lastWriteIndex, -1);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {105}</summary>
        public void ResetIndicesAndData()
        {
            Interlocked.Exchange(ref this.lastWriteIndex, this.Capacity);
            this.ResetValues(this.values);
            this.ResetIndices();
        }

        /// <summary>@ToDo: Complete documentation before stable release. {628}</summary>
        /// <param name="index">@ToDo: Complete documentation before stable release. {939}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {285}</returns>
        public TValue GetAndResetValue(int index)
        {
            TValue value = this.GetAndResetValueOnce(this.values, index);

            if (this.IsInvalidValue(value))
            {
#pragma warning disable SA1129 // Do not use default value type constructor
                var spinWait = new SpinWait();
#pragma warning restore SA1129 // Do not use default value type constructor

                value = this.GetAndResetValueOnce(this.values, index);
                while (this.IsInvalidValue(value))
                {
                    spinWait.SpinOnce();
                    
                    if (spinWait.Count % 100 == 0)
                    {
                        // In tests (including stress tests) we always finished wating before 100 cycles.
                        // However, this is a protection against en extreme case on a slow machine. 
                        Task.Delay(10).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                    }

                    value = this.GetAndResetValueOnce(this.values, index);
                }
            }

            return value;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {016}</summary>
        /// <param name="values">.</param>
        protected abstract void ResetValues(TValue[] values);

        /// <summary>@ToDo: Complete documentation before stable release. {277}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {569}</param>
        /// <param name="index">@ToDo: Complete documentation before stable release. {193}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {571}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract TValue GetAndResetValueOnce(TValue[] values, int index);

        /// <summary>@ToDo: Complete documentation before stable release. {016}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {051}</param>
        /// <param name="index">@ToDo: Complete documentation before stable release. {872}</param>
        /// <param name="value">@ToDo: Complete documentation before stable release. {963}</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void WriteValueOnce(TValue[] values, int index, TValue value);

        /// <summary>@ToDo: Complete documentation before stable release. {997}</summary>
        /// <param name="value">@ToDo: Complete documentation before stable release. {863}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {432}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool IsInvalidValue(TValue value);
    }

    /// <summary>@ToDo: Complete documentation before stable release. {469}</summary>
    /// @PublicExposureCandidate
    internal sealed class MetricValuesBuffer_Double : MetricValuesBufferBase<double>
    {
        /// <summary>@ToDo: Complete documentation before stable release. {923}</summary>
        /// <param name="capacity">@ToDo: Complete documentation before stable release. {808}</param>
        public MetricValuesBuffer_Double(int capacity)
            : base(capacity)
        {
        }

        /// <summary>@ToDo: Complete documentation before stable release. {611}</summary>
        /// <param name="value">@ToDo: Complete documentation before stable release. {523}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {416}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(double value)
        {
            return Double.IsNaN(value);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {768}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {365}</param>
        protected override void ResetValues(double[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], Double.NaN))
            {
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {913}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {943}</param>
        /// <param name="index">@ToDo: Complete documentation before stable release. {130}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {880}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override double GetAndResetValueOnce(double[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], Double.NaN);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {684}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {246}</param>
        /// <param name="index">@ToDo: Complete documentation before stable release. {452}</param>
        /// <param name="value">@ToDo: Complete documentation before stable release. {035}</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(double[] values, int index, double value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }

    /// <summary>@ToDo: Complete documentation before stable release. {201}</summary>
    /// @PublicExposureCandidate
    internal sealed class MetricValuesBuffer_Object : MetricValuesBufferBase<object>
    {
        /// <summary>@ToDo: Complete documentation before stable release. {314}</summary>
        /// <param name="capacity">@ToDo: Complete documentation before stable release. {683}</param>
        public MetricValuesBuffer_Object(int capacity)
            : base(capacity)
        {
        }

        /// <summary>@ToDo: Complete documentation before stable release. {922}</summary>
        /// <param name="value">@ToDo: Complete documentation before stable release. {263}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {284}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsInvalidValue(object value)
        {
            return value == null;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {616}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {931}</param>
        protected override void ResetValues(object[] values)
        {
            for (int i = 0; i < values.Length; Interlocked.Exchange(ref values[i++], null))
            {
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {940}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {673}</param>
        /// <param name="index">@ToDo: Complete documentation before stable release. {249}</param>
        /// <returns>@ToDo: Complete documentation before stable release. {311}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object GetAndResetValueOnce(object[] values, int index)
        {
            return Interlocked.Exchange(ref values[index], null);
        }

        /// <summary>@ToDo: Complete documentation before stable release. {003}</summary>
        /// <param name="values">@ToDo: Complete documentation before stable release. {110}</param>
        /// <param name="index">@ToDo: Complete documentation before stable release. {970}</param>
        /// <param name="value">@ToDo: Complete documentation before stable release. {000}</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void WriteValueOnce(object[] values, int index, object value)
        {
            Interlocked.Exchange(ref values[index], value);
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
#pragma warning restore SA1649 // File name must match first type name
}
