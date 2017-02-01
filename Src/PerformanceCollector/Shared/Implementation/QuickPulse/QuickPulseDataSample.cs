namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helpers;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;

    /// <summary>
    /// DTO containing data that we send to QPS.
    /// </summary>
    internal class QuickPulseDataSample
    {
        public QuickPulseDataSample(QuickPulseDataAccumulator accumulator, IDictionary<string, Tuple<PerformanceCounterData, double>> perfData, IEnumerable<Tuple<string, int>> topCpuData, bool topCpuDataAccessDenied)
        {
            // NOTE: it is crucial not to keep any heap references on input parameters, new objects with separate roots must be created!
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
                throw new InvalidOperationException("StartTimestamp must be less than EndTimestamp.");
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

            this.PerfCountersLookup = perfData.ToDictionary(
                p => (QuickPulseDefaults.CounterOriginalStringMapping.ContainsKey(p.Value.Item1.OriginalString) ? QuickPulseDefaults.CounterOriginalStringMapping[p.Value.Item1.OriginalString] : p.Value.Item1.OriginalString), 
                p => p.Value.Item2);

            this.TopCpuData = topCpuData.ToArray();

            this.TelemetryDocuments = accumulator.TelemetryDocuments.ToArray();

            this.TopCpuDataAccessDenied = topCpuDataAccessDenied;
        }
        
        public DateTimeOffset StartTimestamp { get; }

        public DateTimeOffset EndTimestamp { get; }
        
        #region Aggregates
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

        public IDictionary<string, double> PerfCountersLookup { get; private set; }

        public IEnumerable<Tuple<string, int>> TopCpuData { get; private set; }
            
        public ITelemetryDocument[] TelemetryDocuments { get; private set; }

        public bool TopCpuDataAccessDenied { get; private set; }
    }
}
