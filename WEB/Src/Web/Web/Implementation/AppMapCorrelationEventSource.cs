namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Diagnostics.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web")]
    internal sealed partial class AppMapCorrelationEventSource : EventSource
    {
    }
}
