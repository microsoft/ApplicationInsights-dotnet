namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Encapsulates information about a web request handled by the application.
    /// </summary>
    /// <remarks>
    /// You can send information about requests processed by your web application to Application Insights by 
    /// passing an instance of the <see cref="RequestTelemetry"/> class to the <see cref="TelemetryClient.TrackRequest(RequestTelemetry)"/> 
    /// method.
    /// </remarks>
    public sealed class RequestTelemetry : OperationTelemetry, ITelemetry, ISupportProperties, ISupportSampling
    {
        internal const string TelemetryName = "Request";

        internal readonly string BaseType = typeof(RequestData).Name;
        internal readonly RequestData Data;
        private readonly TelemetryContext context;
        private bool successFieldSet;

        private double samplingPercentage = Constants.DefaultSamplingPercentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class.
        /// </summary>
        public RequestTelemetry()
        {
            this.Data = new RequestData();
            this.context = new TelemetryContext(this.Data.properties, new Dictionary<string, string>());
            this.Id = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTelemetry"/> class with the given <paramref name="name"/>, 
        /// <paramref name="startTime"/>, <paramref name="duration"/>, <paramref name="responseCode"/> and <paramref name="success"/> property values.
        /// </summary>
        public RequestTelemetry(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
            : this()
        {
            this.Name = name; // Name is optional but without it UX does not make much sense
            this.StartTime = startTime;
            this.Duration = duration;
            this.ResponseCode = responseCode;
            this.Success = success;
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public override DateTimeOffset Timestamp
        {
            get { return this.ValidateDateTimeOffset(this.Data.startTime); }
            set { this.Data.startTime = value.ToString("o", CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets or sets the date and time when request was processed by the application.
        /// </summary>
        public override DateTimeOffset StartTime
        {
            get { return this.ValidateDateTimeOffset(this.Data.startTime); }
            set { this.Data.startTime = value.ToString("o", CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public override string Sequence { get; set; }

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
            get { return this.Data.id; }  
            set { this.Data.id = value; }  
        }

        /// <summary>
        /// Gets or sets human-readable name of the requested page.
        /// </summary>
        public override string Name
        {
            get { return this.Data.name; }
            set { this.Data.name = value; }
        }

        /// <summary>
        /// Gets or sets response code returned by the application after handling the request.
        /// </summary>
        public string ResponseCode
        {
            get { return this.Data.responseCode; }
            set { this.Data.responseCode = value; }
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
                    return this.Data.success;
                }

                return null;
            }

            set
            {
                if (value != null && value.HasValue)
                {
                    this.Data.success = value.Value;
                    this.successFieldSet = true;
                }
                else
                {
                    this.Data.success = true;
                    this.successFieldSet = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the amount of time it took the application to handle the request.
        /// </summary>
        public override TimeSpan Duration
        {
            get { return Utils.ValidateDuration(this.Data.duration); }
            set { this.Data.duration = value.ToString(); }
        }

        /// <summary>
        /// Gets a dictionary of application-defined property names and values providing additional information about this request.
        /// </summary>
        public override IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Gets or sets request url (optional).
        /// </summary>
        public Uri Url
        {
            get
            {
                if (this.Data.url.IsNullOrWhiteSpace())
                {
                    return null;
                }

                return new Uri(this.Data.url, UriKind.RelativeOrAbsolute);
            }

            set 
            {
                this.Data.url = value == null ? null : value.ToString();
            }
        }
        
        /// <summary>
        /// Gets a dictionary of application-defined request metrics.
        /// </summary>
        public IDictionary<string, double> Metrics
        {
            get { return this.Data.measurements; }
        }

        /// <summary>
        /// Gets or sets the HTTP method of the request.
        /// </summary>
        public string HttpMethod
        {
            get { return this.Data.httpMethod; }
            set { this.Data.httpMethod = value; }
        }

        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// </summary>
        double ISupportSampling.SamplingPercentage
        {
            get { return this.samplingPercentage; }
            set { this.samplingPercentage = value; }
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            this.Name = this.Name.SanitizeName();
            this.Properties.SanitizeProperties();
            this.Metrics.SanitizeMeasurements();
            this.Url = this.Url.SanitizeUri();

            // Set for backward compatibility:  
            this.Data.id = this.Data.id.SanitizeName();
            this.Data.id = Utils.PopulateRequiredStringValue(this.Data.id, "id", typeof(RequestTelemetry).FullName);

            // Required field
            if (string.IsNullOrEmpty(this.ResponseCode))
            {
                this.ResponseCode = "200";
                this.Success = true;
            }

            // Required field
            if (!this.Success.HasValue)
            {
                int responseCode;

                if (int.TryParse(this.ResponseCode, NumberStyles.Any, CultureInfo.InvariantCulture, out responseCode))
                {
                    this.Success = (responseCode < 400) || (responseCode == 401);
                }
                else
                {
                    this.Success = true;
                }
            }
        }

        private DateTimeOffset ValidateDateTimeOffset(string value)
        {
            DateTimeOffset timestamp;
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timestamp))
            {
                return DateTimeOffset.MinValue;
            }

            return timestamp;
        }
    }
}