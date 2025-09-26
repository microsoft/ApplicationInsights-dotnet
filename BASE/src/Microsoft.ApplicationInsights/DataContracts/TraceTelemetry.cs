namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry type used for log messages.
    /// Contains a time and message and optionally some additional metadata.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#tracktrace">Learn more</a>
    /// </summary>
    public sealed class TraceTelemetry : ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "Message";
        internal string EnvelopeName = "AppTraces";
        private readonly TelemetryContext context;

        private double? samplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class.
        /// </summary>
        public TraceTelemetry()
        {
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class.
        /// </summary>
        public TraceTelemetry(string message) : this()
        {
            this.Message = message;
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class.
        /// </summary>
        public TraceTelemetry(string message, SeverityLevel severityLevel) : this(message)
        {
            this.SeverityLevel = severityLevel;
            this.context = new TelemetryContext();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="TraceTelemetry"/> to clone from.</param>
        private TraceTelemetry(TraceTelemetry source)
        {
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
            this.samplingPercentage = source.samplingPercentage;
            this.context = new TelemetryContext();
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
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Trace severity level.
        /// </summary>
        public SeverityLevel? SeverityLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this trace.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get
            {
                return this.Context.Properties;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Gets or sets the MetricExtractorInfo.
        /// </summary>
        internal string MetricExtractorInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Deeply clones a <see cref="TraceTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new TraceTelemetry(this);
        }
    }
}
