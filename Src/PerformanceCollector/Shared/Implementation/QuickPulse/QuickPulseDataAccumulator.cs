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
        // //!!! what if long overflows? For ticks maybe?
        public long AIRequestCount;
        public long AIRequestDurationInTicks;
        public long AIRequestSuccessCount;
        public long AIRequestFailureCount;

        public long AIDependencyCallCount;
        public long AIDependencyCallDurationInTicks;
        public long AIDependencyCallSuccessCount;
        public long AIDependencyCallFailureCount;
        
        #endregion
    }
}
