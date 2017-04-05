namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Obsolete - use EventTelemetry. Telemetry type used to track user sessions.
    /// </summary>
    [Obsolete("Session state events are no longer used. This telemetry item will be sent as EventTelemetry.")]
    public sealed class SessionStateTelemetry : ITelemetry
    {
        internal readonly EventTelemetry Data;

        private readonly string startEventName = "Session started";
        private readonly string endEventName = "Session ended";

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStateTelemetry"/> class.
        /// </summary>
        public SessionStateTelemetry()
            : this(SessionState.Start)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStateTelemetry"/> class with the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="state">
        /// A <see cref="SessionState"/> value indicating state of the user session.
        /// </param>
        public SessionStateTelemetry(SessionState state)
        {
            this.Data = new EventTelemetry();
            this.State = state;
        }

        /// <summary>
        /// Gets or sets the date and time the session state was recorded.
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get
            {
                return this.Data.Timestamp;
            }

            set
            {
                this.Data.Timestamp = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="TelemetryContext"/> of the application when the session state was recorded.
        /// </summary>
        public TelemetryContext Context
        {
            get { return this.Data.Context; }
        }

        /// <summary>
        /// Gets or sets the value that defines absolute order of the telemetry item.
        /// </summary>
        public string Sequence
        {
            get
            {
                return this.Data.Sequence;
            }

            set
            {
                this.Data.Sequence = value;
            }
        }

        /// <summary>
        /// Gets or sets the value describing state of the user session.
        /// </summary>
        public SessionState State
        {
            get
            {
                if (this.Data.Name == this.startEventName)
                {
                    return SessionState.Start;
                }
                else
                {
                    return SessionState.End;
                }
            }

            set
            {
                if (value == SessionState.Start)
                {
                    this.Data.Name = this.startEventName;
                }
                else
                {
                    this.Data.Name = this.endEventName;
                }
            }
        }

        /// <summary>
        /// Sanitizes this telemetry instance to ensure it can be accepted by the Application Insights.
        /// </summary>
        void ITelemetry.Sanitize()
        {
            ((ITelemetry)this.Data).Sanitize();
        }
    }
}
