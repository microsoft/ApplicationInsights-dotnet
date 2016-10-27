namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    internal interface IPerformanceData
    {
        string OriginalString { get; }

        string ReportAs { get; }

        bool UsesInstanceNamePlaceholder { get; }

        bool IsCustomCounter { get; }

        bool IsInBadState { get; }
    }
}
