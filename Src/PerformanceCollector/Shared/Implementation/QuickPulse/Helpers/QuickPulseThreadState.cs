namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    internal class QuickPulseThreadState
    {
        public volatile bool IsStopRequested = false;
    }
}