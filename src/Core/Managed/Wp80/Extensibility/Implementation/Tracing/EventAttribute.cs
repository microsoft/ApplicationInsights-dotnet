namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

    /// <summary>
    /// EventSource method attribute for Silverlight.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class EventAttribute : Attribute
    {
        public EventAttribute(int eventId)
        {
            this.EventId = eventId;
            this.Level = EventLevel.Informational;
        }

        /// <summary>
        /// Gets Event's ID.
        /// </summary>
        public int EventId { get; private set; }

        /// <summary>
        /// Gets or sets event's severity level: indicates the severity or verbosity of the event.
        /// </summary>
        public EventLevel Level { get; set; }

        /// <summary>
        /// Gets or sets event keywords.
        /// </summary>
        public EventKeywords Keywords { get; set; }

        /// <summary>
        /// Gets or sets event message format.
        /// </summary>
        public string Message { get; set; }
    }
}
