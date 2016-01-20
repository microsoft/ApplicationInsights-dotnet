namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System.Diagnostics;

    internal class PerformanceCounterData
    {
        public PerformanceCounterData(
            string originalString,
            string reportAs,
            PerformanceCounter pc,
            bool usesInstanceNamePlaceholder,
            bool isCustomCounter,
            bool isInBadState)
        {
            this.OriginalString = originalString;
            this.ReportAs = reportAs;
            this.PerformanceCounter = pc;
            this.UsesInstanceNamePlaceholder = usesInstanceNamePlaceholder;
            this.IsCustomCounter = isCustomCounter;
            this.IsInBadState = isInBadState;
        }

        public PerformanceCounterData(string originalString, string reportAs, PerformanceCounter pc, bool usesInstanceNamePlaceholder, bool isCustomCounter)
            : this(originalString, reportAs, pc, usesInstanceNamePlaceholder, isCustomCounter, false)
        {
        }

        public string OriginalString { get; private set; }

        public string ReportAs { get; private set; }

        public PerformanceCounter PerformanceCounter { get; private set; }

        public bool UsesInstanceNamePlaceholder { get; private set; }

        public bool IsCustomCounter { get; private set; }

        public bool IsInBadState { get; private set; }
    }
}
