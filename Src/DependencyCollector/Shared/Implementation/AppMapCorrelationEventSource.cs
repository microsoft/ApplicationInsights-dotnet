namespace Microsoft.ApplicationInsights.Common
{
    using System;
#if NETCORE || NET45
    using System.Diagnostics.Tracing;
#endif
#if NETCORE
    using System.Reflection;
#endif
    using Extensibility.Implementation.Tracing;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Dependency")]
    internal sealed partial class AppMapCorrelationEventSource : EventSource
    {
    }
}
