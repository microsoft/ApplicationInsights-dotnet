namespace Microsoft.ApplicationInsights.Processors
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal sealed class WarmupGate
    {
        internal const int CountThreshold = 10;
        internal const long TimeThresholdMs = 5_000;

        private long constructedTimestamp;
        private int counter;

        internal WarmupGate()
            : this(Stopwatch.GetTimestamp())
        {
        }

        internal WarmupGate(long constructedTimestamp)
        {
            this.constructedTimestamp = constructedTimestamp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasTimeThresholdElapsed(long constructedTimestamp)
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - constructedTimestamp;
            long elapsedMs = (elapsedTicks * 1000) / Stopwatch.Frequency;
            return elapsedMs >= TimeThresholdMs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldFreeze(ref int counter, long constructedTimestamp)
        {
            int count = Interlocked.Increment(ref counter);
            return count >= CountThreshold && HasTimeThresholdElapsed(constructedTimestamp);
        }

        internal bool ShouldFreeze()
        {
            return ShouldFreeze(ref this.counter, this.constructedTimestamp);
        }
    }
}
