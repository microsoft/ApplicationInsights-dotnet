namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// This class is responsible for sending diagnostics information into portal.
    /// </summary>
    internal class PortalDiagnosticsSender : IDiagnosticsSender
    {
        /// <summary>
        /// Prefix of the traces in portal.
        /// </summary>
        private const string AiPrefix = "AI: ";

        /// <summary>
        /// For user non actionable traces use AI Internal prefix.
        /// </summary>
        private const string AiNonUserActionable = "AI (Internal): ";
        
        private readonly TelemetryClient telemetryClient;
        private readonly IDiagnoisticsEventThrottlingManager throttlingManager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PortalDiagnosticsSender"/> class. 
        /// </summary>
        public PortalDiagnosticsSender(
            TelemetryConfiguration configuration,
            IDiagnoisticsEventThrottlingManager throttlingManager)
        {
            if (null == configuration)
            {
                throw new ArgumentNullException("configuration");
            }

            if (null == throttlingManager)
            {
                throw new ArgumentNullException("throttlingManager");
            }

            this.telemetryClient = new TelemetryClient(configuration);

            this.throttlingManager = throttlingManager;
        }

        /// <summary>
        /// Gets or sets instrumentation key for diagnostics (optional).
        /// </summary>
        public string DiagnosticsInstrumentationKey { get; set; }

        public void Send(TraceEvent eventData)
        {
            try
            {
                if (eventData.MetaData != null && !string.IsNullOrEmpty(eventData.MetaData.MessageFormat))
                {
                    // Check if trace message is sended to the portal (somewhere before in the stack)
                    // It allows to avoid infinite recursion if sending to the portal traces something.
                    if (!ThreadResourceLock.IsResourceLocked)
                    {
                        using (var portalSenderLock = new ThreadResourceLock())
                        {
                            try
                            {
                                if (!this.throttlingManager.ThrottleEvent(eventData.MetaData.EventId, eventData.MetaData.Keywords))
                                {
                                    this.InternalSendTraceTelemetry(eventData);
                                }
                            }
                            catch (Exception exp)
                            {
                                // This message will not be sent to the portal because we have infinite loop protection
                                // But it will be available in PerfView or StatusMonitor
                                CoreEventSource.Log.LogError("Failed to send traces to the portal: " + exp);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // We were trying to send traces out and failed. 
                // No reason to try to trace something else again
            }
        }

        private void InternalSendTraceTelemetry(TraceEvent eventData)
        {
            if (this.telemetryClient.TelemetryConfiguration.TelemetryChannel == null)
            {
                return;
            }

            var traceTelemetry = new TraceTelemetry();
            
            string message;
            if (eventData.Payload != null)
            {
                const string ParameterNameFormat = "arg{0}";
                    
                for (int i = 1; i <= eventData.Payload.Count(); i++)
                {
                    traceTelemetry.Properties.Add(
                        string.Format(CultureInfo.CurrentCulture, ParameterNameFormat, i),
                        eventData.Payload[i - 1].ToString());
                }

                message = string.Format(CultureInfo.CurrentCulture, eventData.MetaData.MessageFormat, eventData.Payload.ToArray());
            }
            else
            {
                message = eventData.MetaData.MessageFormat;
            }

            // Add "AI: " prefix (if keyword does not contain UserActionable = (EventKeywords)0x1, than prefix should be "AI (Internal):" )
            if ((eventData.MetaData.Keywords & EventSourceKeywords.UserActionable) == EventSourceKeywords.UserActionable)
            {
                message = AiPrefix + message;
            }
            else
            {
                message = AiNonUserActionable + message;
            }

            traceTelemetry.Message = message;
            if (!string.IsNullOrEmpty(this.DiagnosticsInstrumentationKey))
            {
                traceTelemetry.Context.InstrumentationKey = this.DiagnosticsInstrumentationKey;
            }

            this.telemetryClient.TrackTrace(traceTelemetry);
        }
    }
}
