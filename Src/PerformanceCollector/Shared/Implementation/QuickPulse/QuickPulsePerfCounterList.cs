namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    internal enum QuickPulsePerfCounters
    {
        PerfIisRequestsPerSecond,

        PerfIisRequestDurationAve,

        PerfIisRequestsFailedTotal,

        PerfIisRequestsSucceededTotal,

        PerfIisQueueSize,

        PerfCpuUtilization,

        PerfMemoryInBytes
    }

    /// <summary>
    /// Represents a list of performance counters to collect for QPS.
    /// </summary>
    internal static class QuickPulsePerfCounterList
    {
        public static Tuple<QuickPulsePerfCounters, string>[] CountersToCollect
            =>
                new[]
                    {
                        Tuple.Create(QuickPulsePerfCounters.PerfIisRequestsPerSecond, @"\ASP.NET Applications(__Total__)\Requests/Sec"),
                        Tuple.Create(QuickPulsePerfCounters.PerfIisRequestDurationAve, @"\ASP.NET Applications(__Total__)\Request Execution Time"),
                        Tuple.Create(QuickPulsePerfCounters.PerfIisRequestsFailedTotal, @"\ASP.NET Applications(__Total__)\Requests Failed"),
                        Tuple.Create(QuickPulsePerfCounters.PerfIisRequestsSucceededTotal, @"\ASP.NET Applications(__Total__)\Requests Succeeded"),
                        Tuple.Create(QuickPulsePerfCounters.PerfIisQueueSize, @"\ASP.NET Applications(__Total__)\Requests In Application Queue"),
                        Tuple.Create(QuickPulsePerfCounters.PerfCpuUtilization, @"\Memory\Committed Bytes"),
                        Tuple.Create(QuickPulsePerfCounters.PerfMemoryInBytes, @"\Processor(_Total)\% Processor Time")
                    };
    }
}