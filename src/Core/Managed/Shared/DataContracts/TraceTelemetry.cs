namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Telemetry type used for log messages.
    /// Contains a time and message and optionally some additional metadata.
    /// </summary>
    public sealed class TraceTelemetry : ITelemetry, ISupportProperties, ISupportSampling
    {
        internal const string TelemetryName = "Message";

        internal readonly string BaseType = typeof(MessageData).Name;
        internal readonly MessageData Data;
        private readonly TelemetryContext context;

        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class.
        /// </summary>
        public TraceTelemetry()
        {
            this.Data = new MessageData();
            this.context = new TelemetryContext(this.Data.properties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class.
        /// </summary>
        public TraceTelemetry(string message) : this()
        {
            this.Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class.
        /// </summary>
        public TraceTelemetry(string message, SeverityLevel severityLevel) : this(message)
        {
            this.SeverityLevel = severityLevel;
        }

        /// <summary>
        /// Gets or sets date and time when event was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence { get; set; }

        /// <summary>
        /// Gets the context associated with the current telemetry item.
        /// </summary>
        public TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets the message text. For example, the text that would normally be written to a log file line.
        /// </summary>
        public string Message
        {
            get { return this.Data.message; }
            set { this.Data.message = value; }
        }

        /// <summary>
        /// Gets or sets Trace severity level.
        /// </summary>
        public SeverityLevel? SeverityLevel
        {
            get { return this.Data.severityLevel.TranslateSeverityLevel(); }
            set { this.Data.severityLevel = value.TranslateSeverityLevel(); }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this trace.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// </summary>
        double? ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Data.message = this.Data.message.SanitizeMessage();
            this.Data.message = Utils.PopulateRequiredStringValue(this.Data.message, "message", typeof(TraceTelemetry).FullName);
            this.Data.properties.SanitizeProperties();
        }
    }
}
