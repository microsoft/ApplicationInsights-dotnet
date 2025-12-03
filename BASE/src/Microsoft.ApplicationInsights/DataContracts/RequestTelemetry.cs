namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Encapsulates information about a web request handled by the application.
    /// </summary>
    /// <remarks>
    /// You can send information about requests processed by your web application to Application Insights by
    /// passing an instance of the <see cref="RequestTelemetry"/> class to the <see cref="TelemetryClient.TrackRequest(RequestTelemetry)"/>
    /// method.
    /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#trackrequest">Learn more</a>
    /// </remarks>
    public sealed class RequestTelemetry : OperationTelemetry, ITelemetry, ISupportProperties
    {
        internal const string EtwEnvelopeName = "Request";
        internal string EnvelopeName = "AppRequests";

        private readonly TelemetryContext context;
        private bool successFieldSet;
        private bool success = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class.
        /// </summary>
        public RequestTelemetry()
        {
            this.context = new TelemetryContext();
            this.Source = string.Empty;
            this.Name = string.Empty;
            this.ResponseCode = string.Empty;            
            this.Duration = System.TimeSpan.Zero;
            this.Properties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class with the given <paramref name="name"/>,
        /// <paramref name="startTime"/>, <paramref name="duration"/>, <paramref name="responseCode"/> and <paramref name="success"/> property values.
        /// </summary>
        public RequestTelemetry(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
            : this()
        {
            this.Name = name; // Name is optional but without it UX does not make much sense
            this.Timestamp = startTime;
            this.Duration = duration;
            this.ResponseCode = responseCode;
            this.Success = success;
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public override DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the object that contains contextual information about the application at the time when it handled the request.
        /// </summary>
        public override TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets Request ID.
        /// </summary>
        public override string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets human-readable name of the requested page.
        /// </summary>
        public override string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets response code returned by the application after handling the request.
        /// </summary>
        public string ResponseCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether application handled the request successfully.
        /// </summary>
        public override bool? Success
        {
            get
            {
                if (this.successFieldSet)
                {
                    return this.success;
                }

                return null;
            }

            set
            {
                if (value != null && value.HasValue)
                {
                    this.success = value.Value;
                    this.successFieldSet = true;
                }
                else
                {
                    this.success = true;
                    this.successFieldSet = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the amount of time it took the application to handle the request.
        /// </summary>
        public override TimeSpan Duration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this request.
        /// <a href="https://go.microsoft.com/fwlink/?linkid=525722#properties">Learn more</a>
        /// </summary>
        public override IDictionary<string, string> Properties
        {
            get;
        }

        /// <summary>
        /// Gets or sets request url (optional).
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets the source for the request telemetry object. This often is a hashed instrumentation key identifying the caller.
        /// </summary>
        public string Source
        {
            get;
            set;
        }
    }
}
