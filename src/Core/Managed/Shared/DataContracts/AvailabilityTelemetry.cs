namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using System.Globalization;

    /// <summary>
    /// Telemetry type used for availability test results.
    /// Contains a time and message and optionally some additional metadata.
    /// </summary>
    public sealed class AvailabilityTelemetry : ITelemetry, ISupportProperties //TODO: Should this implement IOperation like RequestTelemetry?
    {
        internal const string TelemetryName = "Availability";

        internal readonly string BaseType = typeof(AvailabilityData).Name;
        internal readonly AvailabilityData Data;
        private readonly TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailabilityTelemetry"/> class with empty properties.
        /// </summary>
        public AvailabilityTelemetry()
        {
            this.Data = new AvailabilityData();
            this.context = new TelemetryContext(this.Data.properties);
            this.Data.testRunId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailabilityTelemetry"/> class with empty properties.
        /// </summary>
        public AvailabilityTelemetry(string testName, DateTimeOffset testTimeStamp, TimeSpan duration, string runLocation, bool success, string message = null)
        {
            this.Data = new AvailabilityData();
            this.context = new TelemetryContext(this.Data.properties);            
            this.Data.testTimeStamp = testTimeStamp.ToString("o", CultureInfo.InvariantCulture);
            this.Data.testName = testName;
            this.Data.testRunId = Guid.NewGuid().ToString();
            this.Data.duration = duration.ToString(string.Empty, CultureInfo.InvariantCulture);
            this.Data.result = success ? TestResult.Pass : TestResult.Fail;
            this.Data.runLocation = runLocation;
            this.Data.message = message;           
        }        

        /// <summary>
        /// Gets or sets the test run id guid
        /// </summary>
        public Guid Id
        {
            get { return Guid.Parse(this.Data.testRunId); }
            set { this.Data.testRunId = value.ToString(); }
        }

        /// <summary>
        /// Gets or sets date and time when the availability test was executed.
        /// </summary>
        public DateTimeOffset TestTimeStamp
        {
            get { return Utils.ValidateDateTimeOffset(this.Data.testTimeStamp); }
            set { this.Data.testTimeStamp = value.ToString("o", CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets or sets the test name.
        /// </summary>
        public string TestName
        {
            get { return this.Data.testName; }
            set { this.Data.testName = value; }
        }

        /// <summary>
        /// Gets or sets availability test duration.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                //TODO: Note: this ends up logging an error messsage for Request Telemetry if it fails, Both PageView and Request telemetry use this to validate so PV already has the bug.
                return Utils.ValidateDuration(this.Data.duration);
            }
            set
            {
                this.Data.duration = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the result of the availability test.
        /// </summary>
        public bool Success
        {
            get { return (this.Data.result == TestResult.Pass) ? true : false; }            
            set { this.Data.result = value ? TestResult.Pass : TestResult.Fail; }
        }

        /// <summary>
        /// Gets or sets location where availability test was run.
        /// </summary>
        public string RunLocation
        {
            get { return this.Data.runLocation; }
            set { this.Data.runLocation = value; }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message
        {
            get { return this.Data.message; }
            set { this.Data.message = value; }
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
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return this.Data.properties; }
        }

        /// <summary>
        /// Gets or sets date and time when telemetry was recorded.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get; set;
        }

        /// <summary>
        /// Sanitizes the properties based on constraints.
        /// </summary>
        void ITelemetry.Sanitize()
        {            
            //TODO: It looks like the oob Web Tests on the portal are populating this property now so adding and ensuring its set to false as we don't support sending full test results.
            if (this.Data.properties.ContainsKey("FullTestResultAvailable"))
                this.Data.properties["FullTestResultAvailable"] = "false";
            else
                this.Data.properties.Add("FullTestResultAvailable", "false");

            //Makes message content similar to OOB web test results on the portal.
            this.Message = (this.Data.result == TestResult.Pass && String.IsNullOrEmpty(this.Message)) ? "Passed" : ((this.Data.result == TestResult.Fail && String.IsNullOrEmpty(this.Message)) ? "Failed" : this.Message);

            this.TestName = this.TestName.SanitizeTestName();
            this.TestName = Utils.PopulateRequiredStringValue(this.TestName, "TestName", typeof(AvailabilityTelemetry).FullName);

            this.RunLocation = this.RunLocation.SanitizeRunLocation();
            this.Message = this.Message.SanitizeAvailabilityMessage();

            this.Data.properties.SanitizeProperties();
        }
    }
}
