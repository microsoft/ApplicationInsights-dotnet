namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.Helpers
{
    internal class QuickPulseThreadState
    {
        public volatile bool IsStopRequested = false;
    }
}