namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Linq;

    /// <summary>
    /// DTO containing data that we send to QPS.
    /// </summary>
    internal class QuickPulseDataSample
    {
        public QuickPulseDataSample(QuickPulseDataAccumulator accumulator, ILookup<string, float> perfData)
        {
            if (!accumulator.EndTimestamp.HasValue)
            {
                throw new ArgumentNullException(nameof(accumulator.EndTimestamp));    
            }

            this.StartTimestamp = accumulator.StartTimestamp;
            this.EndTimestamp = accumulator.EndTimestamp.Value;

            // below we're utilizing a little-known fact that any operator defined on a type will produce null if invoked on 
            // a nullable instance of that type which is currently null
            TimeSpan? sampleDuration = this.EndTimestamp - this.StartTimestamp;

            this.AIRequestsPerSecond = accumulator.AIRequestCount / sampleDuration?.TotalSeconds ?? 0;
            this.AIRequestDurationAve = accumulator.AIRequestCount > 0 ? accumulator.AIRequestDurationInTicks / accumulator.AIRequestCount : 0;
            this.AIRequestsFailedPerSecond = accumulator.AIRequestFailureCount / sampleDuration?.TotalSeconds ?? 0;
            this.AIRequestsSucceededPerSecond = accumulator.AIRequestSuccessCount / sampleDuration?.TotalSeconds ?? 0;

            this.AIDependencyCallsPerSecond = accumulator.AIDependencyCallCount / sampleDuration?.TotalSeconds ?? 0;
            this.AIDependencyCallDurationAve = accumulator.AIDependencyCallCount > 0 ? accumulator.AIDependencyCallDurationInTicks / accumulator.AIDependencyCallCount : 0;
            this.AIDependencyCallsFailedPerSecond = accumulator.AIDependencyCallFailureCount / sampleDuration?.TotalSeconds ?? 0;
            this.AIDependencyCallsSucceededPerSecond = accumulator.AIDependencyCallSuccessCount / sampleDuration?.TotalSeconds ?? 0;

            // avoiding reflection (Enum.GetNames()) to speed things up
            this.PerfIisRequestsPerSecond = perfData[QuickPulsePerfCounters.PerfIisRequestsPerSecond.ToString()].SingleOrDefault();
            this.PerfIisRequestDurationAve = perfData[QuickPulsePerfCounters.PerfIisRequestDurationAve.ToString()].SingleOrDefault();
            this.PerfIisRequestsFailedTotal = perfData[QuickPulsePerfCounters.PerfIisRequestsFailedTotal.ToString()].SingleOrDefault();
            this.PerfIisRequestsSucceededTotal = perfData[QuickPulsePerfCounters.PerfIisRequestsSucceededTotal.ToString()].SingleOrDefault();
            this.PerfIisQueueSize = perfData[QuickPulsePerfCounters.PerfIisQueueSize.ToString()].SingleOrDefault();
            this.PerfCpuUtilization = perfData[QuickPulsePerfCounters.PerfCpuUtilization.ToString()].SingleOrDefault();
            this.PerfMemoryInBytes = perfData[QuickPulsePerfCounters.PerfMemoryInBytes.ToString()].SingleOrDefault();
        }

        public DateTime? StartTimestamp;

        public DateTime EndTimestamp;
        
        #region AI
        public double AIRequestsPerSecond { get; private set; }
        public double AIRequestDurationAve { get; private set; }
        public double AIRequestsFailedPerSecond { get; private set; }
        public double AIRequestsSucceededPerSecond { get; private set; }
        

        public double AIDependencyCallsPerSecond { get; private set; }
        public double AIDependencyCallDurationAve { get; private set; }
        public double AIDependencyCallsFailedPerSecond { get; private set; }
        public double AIDependencyCallsSucceededPerSecond { get; private set; }
        #endregion

        #region Performance counters
        public double PerfIisRequestsPerSecond { get; private set; }
        public double PerfIisRequestDurationAve { get; private set; }
        public double PerfIisRequestsFailedTotal { get; private set; }
        public double PerfIisRequestsSucceededTotal { get; private set; }
        public double PerfIisQueueSize { get; private set; }
        public double PerfCpuUtilization { get; private set; }
        public double PerfMemoryInBytes { get; private set; }
        #endregion
    }
}
