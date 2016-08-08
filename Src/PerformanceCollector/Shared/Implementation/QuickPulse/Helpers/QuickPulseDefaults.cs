namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    internal static class QuickPulseDefaults
    {
        private static readonly Uri QuickPulseServiceEndpoint = new Uri("https://rt.services.visualstudio.com/QuickPulseService.svc");

        private static readonly string[] PerformanceCountersToCollect =
            {
                @"\ASP.NET Applications(__Total__)\Requests In Application Queue",
                @"\Memory\Committed Bytes", @"\Processor(_Total)\% Processor Time"
            };

        public static Uri ServiceEndpoint
        {
            get
            {
                return QuickPulseServiceEndpoint;
            }
        }

        public static string[] CountersToCollect
        {
            get
            {
                return PerformanceCountersToCollect;
            }
        }
    }
}