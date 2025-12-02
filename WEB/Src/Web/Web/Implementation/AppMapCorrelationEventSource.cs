namespace Microsoft.ApplicationInsights.Common
{
    using System.Diagnostics.Tracing;

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
#if REDFIELD
    [EventSource(Name = "Redfield-Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web")]
#else
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation-Web")]
#endif
    internal sealed partial class AppMapCorrelationEventSource : EventSource
    {
    }
}
