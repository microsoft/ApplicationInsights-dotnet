namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Diagnostics.Tracing;
#if NETSTANDARD1_6
    using System.Reflection;
#endif
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web")]
    internal sealed partial class AppMapCorrelationEventSource : EventSource
    {
    }
}
