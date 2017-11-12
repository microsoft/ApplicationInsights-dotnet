using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class MetricValuesBuffer<TMetricValue>
    {
        private const int SizeLimit = 50;

        private int _count = 0;
        private readonly TMetricValue[] _values = new TMetricValue[SizeLimit];

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TMetricValue metricValue)
        {
            int nextCount = Interlocked.Increment(ref _count);

            if (nextCount >= SizeLimit)
            {
                return false;
            }

            _values[nextCount - 1] = metricValue;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TMetricValue Get(int index)
        {
            return _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Interlocked.Exchange(ref _count, 0);
        }

    }
}
