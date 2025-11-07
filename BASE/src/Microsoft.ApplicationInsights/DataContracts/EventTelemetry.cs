namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry type used to track custom events.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackevent">Learn more</a>
    /// </summary>
    public sealed class EventTelemetry : ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "Event";
        internal const string DefaultEnvelopeName = "AppEvents";
        internal string EnvelopeName = DefaultEnvelopeName;
        private readonly TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTelemetry"/> class.
        /// </summary>
        public EventTelemetry()
        {
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTelemetry"/> class with the given <paramref name="name"/>.
        /// </summary>
        public EventTelemetry(string name) : this()
        {
            this.Name = name;
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Gets or sets date and time when event was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a dictionary of application-defined event metrics.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, double> Metrics
        {
            get;
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this event.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get;
        }
    }
}
