namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.Helpers
{
    using System;
    using System.Collections.Generic;

    internal static class QuickPulseDefaults
    {
        private static readonly Uri QuickPulseServiceEndpoint = new Uri("https://rt.services.visualstudio.com/QuickPulseService.svc");

        private static readonly Dictionary<QuickPulseCounter, string> PerformanceCountersToCollect = new Dictionary<QuickPulseCounter, string>
            {
                [QuickPulseCounter.Bytes] = @"\Memory\Committed Bytes",
                [QuickPulseCounter.ProcessorTime] = @"\Processor(_Total)\% Processor Time"
            };

       private static readonly Dictionary<QuickPulseCounter, string> WebAppPerformanceCountersToCollect = new Dictionary<QuickPulseCounter, string>
            {
                [QuickPulseCounter.Bytes] = @"\Process(??APP_WIN32_PROC??)\Private Bytes",
                [QuickPulseCounter.ProcessorTime] = @"\Process(??APP_WIN32_PROC??)\% Processor Time"
            };

        private static readonly Dictionary<string, string> WebAppToStandardCounterMapping = new Dictionary<string, string>
        {
            [WebAppPerformanceCountersToCollect[QuickPulseCounter.Bytes]] = PerformanceCountersToCollect[QuickPulseCounter.Bytes],
            [WebAppPerformanceCountersToCollect[QuickPulseCounter.ProcessorTime]] = PerformanceCountersToCollect[QuickPulseCounter.ProcessorTime],
        };

        public static Uri ServiceEndpoint
        {
            get
            {
                return QuickPulseServiceEndpoint;
            }
        }

        public static Dictionary<QuickPulseCounter, string> CountersToCollect
        {
            get
            {
                return PerformanceCounterUtility.IsWebAppRunningInAzure() ? WebAppPerformanceCountersToCollect : PerformanceCountersToCollect;
            }
        }

        public static Dictionary<string, string> CounterOriginalStringMapping
        {
            get
            {
                return WebAppToStandardCounterMapping;
            }
        }
    }
}