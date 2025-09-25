namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Telemetry type used for availability web test results.
    /// Contains a time and message and optionally some additional metadata.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=517889">Learn more</a>
    /// </summary>
    public sealed class AvailabilityTelemetry : ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "Availability";
        internal string EnvelopeName = "AppAvailabilityResults";
        private readonly TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailabilityTelemetry"/> class with empty properties.
        /// </summary>
        public AvailabilityTelemetry()
        {
            this.Success = true;
        }

        // <summary>
        // Initializes a new instance of the <see cref="AvailabilityTelemetry"/> class with empty properties.
        // </summary>
        /*public AvailabilityTelemetry(string name, DateTimeOffset timeStamp, TimeSpan duration, string runLocation, bool success, string message = null)
            : this()
        {
            this.Data.name = name;
            this.Data.duration = duration;
            this.Data.success = success;
            this.Data.runLocation = runLocation;
            this.Data.message = message;
            this.Timestamp = timeStamp;
        }*/

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailabilityTelemetry"/> class by cloning an existing instance.
        /// </summary>
        /// <param name="source">Source instance of <see cref="AvailabilityTelemetry"/> to clone from.</param>
        private AvailabilityTelemetry(AvailabilityTelemetry source)
        {
            this.Sequence = source.Sequence;
            this.Timestamp = source.Timestamp;
        }

        /// <summary>
        /// Gets or sets the test run id.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the test name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets availability test duration.
        /// </summary>
        public TimeSpan Duration
        {
            get;

            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the availability test was successful or not.
        /// </summary>
        public bool Success
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets location where availability test was run.
        /// </summary>
        public string RunLocation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message
        {
            get;
            set;
        }

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
        /// Gets a dictionary of application-defined property names and values providing additional information about this availability test run.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get;
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
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get; set;
        }

        /// <summary>
        /// Deeply clones an  <see cref="AvailabilityTelemetry"/> object.
        /// </summary>
        /// <returns>A cloned instance.</returns>
        public ITelemetry DeepClone()
        {
            return new AvailabilityTelemetry(this);
        }
    }
}
