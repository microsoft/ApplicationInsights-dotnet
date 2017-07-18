using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    public enum MetricConsumerKind : Int32
    {
        Default,
        QuickPulse,
        Custom
    }
}
