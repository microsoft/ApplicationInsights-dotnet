namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
#if NET40 || NET35
    using Microsoft.Diagnostics.Tracing;
#endif
#if CORE_PCL || NET45 || WINRT
    using System.Diagnostics.Tracing;
#endif

    /// <summary>
    /// Event metadata from event source method attribute.
    /// </summary>
    internal class EventMetaData
    {
        public int EventId { get; set; }

        public string MessageFormat { get; set; }

        public long Keywords { get; set; }

        public EventLevel Level { get; set; }
    }
}
