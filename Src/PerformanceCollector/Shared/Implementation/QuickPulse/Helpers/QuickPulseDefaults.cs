namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    internal static class QuickPulseDefaults
    {
        private static readonly Uri QuickPulseServiceEndpoint = new Uri("https://rt.services.visualstudio.com/QuickPulseService.svc");

        public static Uri ServiceEndpoint
        {
            get
            {
                return QuickPulseServiceEndpoint;
            }
        }

        public static string[] CountersToCollect
            =>
                new[]
                    {
                        @"\ASP.NET Applications(__Total__)\Requests In Application Queue",
                        @"\Memory\Committed Bytes",
                        @"\Processor(_Total)\% Processor Time"
                    };
    }
}