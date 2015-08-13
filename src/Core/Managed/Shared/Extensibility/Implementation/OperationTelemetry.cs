namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using DataContracts;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Base class for telemetry types representing duration in time.
    /// </summary>
    public abstract class OperationTelemetry : ITelemetry, ISupportProperties
    {
        /// <summary>
        /// Gets or sets the start time of the operation.
        /// </summary>
        public abstract DateTimeOffset StartTime { get; set;  }

        /// <summary>
        /// Gets or sets the name of the operation.
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Gets or sets whether operaiton has finished successfully.
        /// </summary>
        public abstract bool? Success { get; set; }

        /// <summary>
        /// Gets or sets the duration of the operaiton.
        /// </summary>
        public abstract TimeSpan Duration { get; set;  }

        /// <summary>
        /// Gets the custom properties collection.
        /// </summary>
        public abstract IDictionary<string, string> Properties { get; }

        /// <summary>
        /// Gets or sets the timestamp for the operation.
        /// </summary>
        public abstract DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the object that contains contextual information about the application at the time when it handled the request.
        /// </summary>
        public abstract TelemetryContext Context { get; }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public abstract string Sequence { get; set; }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
        }
    }
}
