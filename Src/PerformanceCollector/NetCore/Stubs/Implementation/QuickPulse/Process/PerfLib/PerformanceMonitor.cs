namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.PerfLib
{
    internal class PerformanceMonitor
    {
        private static readonly byte[] emptyResult = new byte[0];

        public void Close()
        {
        }

        public byte[] GetData(string categoryIndex)
        {
            return PerformanceMonitor.emptyResult;
        }
    }
}
