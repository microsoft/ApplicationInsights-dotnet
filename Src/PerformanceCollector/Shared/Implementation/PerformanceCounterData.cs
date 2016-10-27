namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    internal class PerformanceCounterData : IPerformanceData
    {
        public PerformanceCounterData(
            string originalString,
            string reportAs,
            bool usesInstanceNamePlaceholder,
            bool isCustomCounter,
            bool isInBadState,
            string categoryName,
            string counterName,
            string instanceName)
        {
            this.OriginalString = originalString;
            this.ReportAs = reportAs;
            this.UsesInstanceNamePlaceholder = usesInstanceNamePlaceholder;
            this.IsCustomCounter = isCustomCounter;
            this.IsInBadState = isInBadState;
            this.CategoryName = categoryName;
            this.CounterName = counterName;
            this.InstanceName = instanceName;
        }

        public string OriginalString { get; private set; }

        public string ReportAs { get; private set; }

        public bool UsesInstanceNamePlaceholder { get; private set; }

        public bool IsCustomCounter { get; private set; }

        public bool IsInBadState { get; private set; }

        public string CategoryName { get; private set; }

        public string CounterName { get; private set; }

        public string InstanceName { get; private set; }
    }
}
