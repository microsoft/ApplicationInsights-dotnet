namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    /// <summary>
    /// DTO containing everything that we send to QPS.
    /// </summary>
    /// <remarks>This is performance-critical DTO that needs to be quickly accessed in a thread-safe manner.</remarks>
    internal class QuickPulseDataSample
    {
        public DateTime StartTimestamp;
        
        #region AI
        // //!!! what if int overflows?
        public int AIRequestCount;
        public long AIRequestDurationTicks;
        public int AIRequestSuccessCount;
        public int AIRequestFailureCount;

        public int AIDependencyCallCount;
        public long AIDependencyCallDurationTicks;
        public long AIDependencyCallSuccessCount;
        public long AIDependencyCallFailureCount;
        
        #endregion

        #region Performance counters

        public int PerfIisRequestCount;
        public long PerfIisRequestDurationTicks;
        public long PerfIisRequestFailureCount;

        public double PerfIisQueueSize;
        public double PerfCpuUtilization;
        public double PerfMemoryMBytes;

        #endregion
    }
}
