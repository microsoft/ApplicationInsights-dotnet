namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    /// <summary>
    /// DTO containing data we collect from AI. Modified in real time.
    /// </summary>
    /// <remarks>This is performance-critical DTO that needs to be quickly accessed in a thread-safe manner.</remarks>
    internal class QuickPulseDataAccumulator
    {
        public DateTime? StartTimestamp = null;

        public DateTime? EndTimestamp = null;

        #region AI

        // MSB for the sign, 19 bits for request count, 44 LSBs for duration in ticks
        public long AIRequestCountAndDurationInTicks;

        public long AIRequestSuccessCount;

        public long AIRequestFailureCount;

        // MSB for the sign, 19 bits for dependency call count, 44 LSBs for duration in ticks
        public long AIDependencyCallCountAndDurationInTicks;

        public long AIDependencyCallSuccessCount;

        public long AIDependencyCallFailureCount;

        #endregion

        /// <summary>
        /// 2^19 - 1
        /// </summary>
        private const long MaxCount = 524287;

        /// <summary>
        /// 2^44 - 1
        /// </summary>
        private const long MaxDuration = 17592186044415;

        public static long EncodeCountAndDuration(long count, long duration)
        {
            if (count > MaxCount || duration > MaxDuration)
            {
                // this should never happen, but better have a 0 than garbage
                return 0;
            }

            return (count << 44) + duration;
        }

        public static Tuple<long, long> DecodeCountAndDuration(long countAndDuration)
        {
            return Tuple.Create(countAndDuration >> 44, countAndDuration & MaxDuration);
        }

        public long AIRequestCount => QuickPulseDataAccumulator.DecodeCountAndDuration(this.AIRequestCountAndDurationInTicks).Item1;

        public long AIRequestDurationInTicks => QuickPulseDataAccumulator.DecodeCountAndDuration(this.AIRequestCountAndDurationInTicks).Item2;

        public long AIDependencyCallCount => QuickPulseDataAccumulator.DecodeCountAndDuration(this.AIDependencyCallCountAndDurationInTicks).Item1;

        public long AIDependencyCallDurationInTicks => QuickPulseDataAccumulator.DecodeCountAndDuration(this.AIDependencyCallCountAndDurationInTicks).Item2;
    }
}
