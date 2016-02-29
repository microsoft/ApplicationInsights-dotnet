namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// DTO containing data that we send to QPS.
    /// </summary>
    internal class QuickPulseDataSample
    {
        public QuickPulseDataSample(QuickPulseDataAccumulator accumulator, IDictionary<string, Tuple<PerformanceCounterData, float>> perfData)
        {
            if (accumulator == null)
            {
                throw new ArgumentNullException(nameof(accumulator));
            }

            if (perfData == null)
            {
                throw new ArgumentNullException(nameof(perfData));
            }

            if (accumulator.StartTimestamp == null)
            {
                throw new ArgumentNullException(nameof(accumulator.StartTimestamp));
            }

            if (accumulator.EndTimestamp == null)
            {
                throw new ArgumentNullException(nameof(accumulator.EndTimestamp));
            }

            this.StartTimestamp = accumulator.StartTimestamp.Value;
            this.EndTimestamp = accumulator.EndTimestamp.Value;

            if ((this.EndTimestamp - this.StartTimestamp) < TimeSpan.Zero)
            {
                throw new InvalidOperationException("StartTimestamp must be lesser than EndTimestamp.");
            }

            TimeSpan sampleDuration = this.EndTimestamp - this.StartTimestamp;

            Tuple<long, long> requestCountAndDuration = QuickPulseDataAccumulator.DecodeCountAndDuration(accumulator.AIRequestCountAndDurationInTicks);
            long requestCount = requestCountAndDuration.Item1;
            long requestDurationInTicks = requestCountAndDuration.Item2;

            this.AIRequests = (int)requestCount;
            this.AIRequestsPerSecond = sampleDuration.TotalSeconds > 0 ? requestCount / sampleDuration.TotalSeconds : 0;
            this.AIRequestDurationAveInMs = requestCount > 0 ? (double)requestDurationInTicks / TimeSpan.TicksPerMillisecond / requestCount : 0;
            this.AIRequestsFailedPerSecond = sampleDuration.TotalSeconds > 0 ? accumulator.AIRequestFailureCount / sampleDuration.TotalSeconds : 0;
            this.AIRequestsSucceededPerSecond = sampleDuration.TotalSeconds > 0 ? accumulator.AIRequestSuccessCount / sampleDuration.TotalSeconds : 0;

            Tuple<long, long> dependencyCountAndDuration = QuickPulseDataAccumulator.DecodeCountAndDuration(accumulator.AIDependencyCallCountAndDurationInTicks);
            long dependencyCount = dependencyCountAndDuration.Item1;
            long dependencyDurationInTicks = dependencyCountAndDuration.Item2;

            this.AIDependencyCalls = (int)dependencyCount;
            this.AIDependencyCallsPerSecond = sampleDuration.TotalSeconds > 0 ? dependencyCount / sampleDuration.TotalSeconds : 0;
            this.AIDependencyCallDurationAveInMs = dependencyCount > 0 ? (double)dependencyDurationInTicks / TimeSpan.TicksPerMillisecond / dependencyCount : 0;
            this.AIDependencyCallsFailedPerSecond = sampleDuration.TotalSeconds > 0 ? accumulator.AIDependencyCallFailureCount / sampleDuration.TotalSeconds : 0;
            this.AIDependencyCallsSucceededPerSecond = sampleDuration.TotalSeconds > 0 ? accumulator.AIDependencyCallSuccessCount / sampleDuration.TotalSeconds : 0;

            this.AIExceptionsPerSecond = sampleDuration.TotalSeconds > 0 ? accumulator.AIExceptionCount / sampleDuration.TotalSeconds : 0;

            // avoiding reflection (Enum.GetNames()) to speed things up
            Tuple<PerformanceCounterData, float> value;

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfIisRequestsPerSecond.ToString(), out value))
            {
                this.PerfIisRequestsPerSecond = value.Item2;
            }

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfIisRequestDurationAve.ToString(), out value))
            {
                this.PerfIisRequestDurationAveInTicks = value.Item2;
            }

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfIisRequestsFailedTotal.ToString(), out value))
            {
                this.PerfIisRequestsFailedTotal = value.Item2;
            }

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfIisRequestsSucceededTotal.ToString(), out value))
            {
                this.PerfIisRequestsSucceededTotal = value.Item2;
            }

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfIisQueueSize.ToString(), out value))
            {
                this.PerfIisQueueSize = value.Item2;
            }

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfCpuUtilization.ToString(), out value))
            {
                this.PerfCpuUtilization = value.Item2;
            }

            if (perfData.TryGetValue(QuickPulsePerfCounters.PerfMemoryInBytes.ToString(), out value))
            {
                this.PerfMemoryInBytes = value.Item2;
            }

            this.PerfCountersLookup = perfData.ToDictionary(p => p.Value.Item1.OriginalString, p => p.Value.Item2);
        }
        
        public DateTimeOffset StartTimestamp { get; }

        public DateTimeOffset EndTimestamp { get; }
        
        #region AI
        public int AIRequests { get; private set; }

        public double AIRequestsPerSecond { get; private set; }

        public double AIRequestDurationAveInMs { get; private set; }

        public double AIRequestsFailedPerSecond { get; private set; }

        public double AIRequestsSucceededPerSecond { get; private set; }
        
        public int AIDependencyCalls { get; private set; }

        public double AIDependencyCallsPerSecond { get; private set; }

        public double AIDependencyCallDurationAveInMs { get; private set; }

        public double AIDependencyCallsFailedPerSecond { get; private set; }

        public double AIDependencyCallsSucceededPerSecond { get; private set; }

        public double AIExceptionsPerSecond { get; private set; }

        #endregion

        #region Performance counters

        public IDictionary<string, float> PerfCountersLookup { get; private set; }

        public double PerfIisRequestsPerSecond { get; private set; }

        public double PerfIisRequestDurationAveInTicks { get; private set; }

        public double PerfIisRequestsFailedTotal { get; private set; }

        public double PerfIisRequestsSucceededTotal { get; private set; }

        public double PerfIisQueueSize { get; private set; }

        public double PerfCpuUtilization { get; private set; }

        public double PerfMemoryInBytes { get; private set; }

        #endregion
    }
}
