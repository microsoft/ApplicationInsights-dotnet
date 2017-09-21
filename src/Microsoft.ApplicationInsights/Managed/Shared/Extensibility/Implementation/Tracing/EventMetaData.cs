namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Diagnostics.Tracing;

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
