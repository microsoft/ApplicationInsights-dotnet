namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Telemetry type used to track user sessions.
    /// </summary>
    public sealed class SessionStateTelemetry : ITelemetry
    {
        internal const string TelemetryName = "SessionState";

        internal readonly SessionStateData Data;
        private readonly TelemetryContext context;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStateTelemetry"/> class.
        /// </summary>
        public SessionStateTelemetry()
        {
            this.Data = new SessionStateData();
            this.context = new TelemetryContext(new Dictionary<string, string>(), new Dictionary<string, string>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStateTelemetry"/> class with the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="state">
        /// A <see cref="SessionState"/> value indicating state of the user session.
        /// </param>
        public SessionStateTelemetry(SessionState state) : this()
        {
            this.State = state;
        }

        /// <summary>
        /// Gets or sets the date and time the session state was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets the <see cref="TelemetryContext"/> of the application when the session state was recorded.
        /// </summary>
        public TelemetryContext Context
        {
            get { return this.context; }
        }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence { get; set; }

        /// <summary>
        /// Gets or sets the value describing state of the user session.
        /// </summary>
        public SessionState State { get; set; }

        /// <summary>
        /// Sanitizes this telemetry instance to ensure it can be accepted by the Application Insights.
        /// </summary>
        void ITelemetry.Sanitize()
        {
        }
    }
}
