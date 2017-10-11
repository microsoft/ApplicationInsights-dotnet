namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implementation of health heartbeat functionality.
    /// </summary>
    internal class HealthHeartbeatProvider : IDisposable, IHeartbeatProvider
    {
        /// <summary>
        /// The default interval between heartbeats if not specified by the user
        /// </summary>
        public static int DefaultHeartbeatIntervalMs = 5000;

        /// <summary>
        /// The default fields to include in every heartbeat sent. Note that setting the value to '*' includes all default fields.
        /// </summary>
        public static string DefaultAllowedFieldsInHeartbeatPayload = "*";

        public static string HeartbeatSyntheticMetricName = "SDKHeartbeat";

        private bool disposedValue = false; // To detect redundant calls to dispose
        private int intervalBetweenHeartbeatsMs; // time between heartbeats emitted specified in milliseconds
        private string enabledHeartbeatPayloadFields; // string containing fields that are enabled in the payload. * means everything available.
        private TelemetryClient telemetryClient; // client to use in sending our heartbeat

        public HealthHeartbeatProvider() : this(DefaultHeartbeatIntervalMs, DefaultAllowedFieldsInHeartbeatPayload)
        {
        }

        public HealthHeartbeatProvider(int delayMs) : this(delayMs, DefaultAllowedFieldsInHeartbeatPayload)
        {
        }

        public HealthHeartbeatProvider(string allowedPayloadFields) : this(DefaultHeartbeatIntervalMs, allowedPayloadFields)
        {
        }

        public HealthHeartbeatProvider(int delayMs, string allowedPayloadFields)
        {
            this.enabledHeartbeatPayloadFields = allowedPayloadFields;
            this.intervalBetweenHeartbeatsMs = delayMs;
        }

        public int HeartbeatIntervalMs => this.intervalBetweenHeartbeatsMs;

        public string EnabledPayloadFields => this.enabledHeartbeatPayloadFields;

        public bool Initialize(TelemetryConfiguration configuration, int? delayMs = null, string allowedPayloadFields = null)
        {
            this.telemetryClient = new TelemetryClient(configuration);

            this.intervalBetweenHeartbeatsMs = delayMs.GetValueOrDefault(this.intervalBetweenHeartbeatsMs);

            if (!string.IsNullOrEmpty(allowedPayloadFields))
            {
                this.enabledHeartbeatPayloadFields = allowedPayloadFields;
            }

            return true;
        }

        public void RegisterHeartbeatPayload(IHealthHeartbeatPayloadExtension payloadProvider)
        {
            if (payloadProvider == null)
            {
                throw new ArgumentNullException(nameof(payloadProvider));
            }

            throw new NotImplementedException();
        }

        public bool UpdateSettings()
        {
            return true;
        }

        #region IDisposable Support

        // Override the finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HealthHeartbeatProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                this.disposedValue = true;
            }
        }

        #endregion

        protected void Send()
        {
            this.SendHealthHeartbeat();
        }

        protected MetricTelemetry GatherData()
        {
            return this.GatherDataForHeartbeatPayload();
        }

        private MetricTelemetry GatherDataForHeartbeatPayload()
        {
            throw new NotImplementedException();
        }

        private void SendHealthHeartbeat()
        {
            try
            {
                var heartbeatPayload = this.GatherData();
                if (heartbeatPayload.Properties != null && heartbeatPayload.Count > 0)
                {
                    // Check if message is sent to the portal (somewhere before in the stack)
                    // It allows to avoid infinite recursion if sending to the portal traces something.
                    if (!ThreadResourceLock.IsResourceLocked)
                    {
                        using (var portalSenderLock = new ThreadResourceLock())
                        {
                            try
                            {
                                this.InternalSendHealthHeartbeat(heartbeatPayload);
                            }
                            catch (Exception exp)
                            {
                                // This message will not be sent to the portal because we have infinite loop protection
                                // But it will be available in PerfView or StatusMonitor
                                CoreEventSource.Log.LogError("Failed to send traces to the portal: " + exp.ToInvariantString());
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

        private void InternalSendHealthHeartbeat(MetricTelemetry eventData)
        {
            if (this.telemetryClient.TelemetryConfiguration.TelemetryChannel == null)
            {
                return;
            }

            eventData.Context.Operation.SyntheticSource = HeartbeatSyntheticMetricName;

            this.telemetryClient.TrackMetric(eventData);
        }
    }
}
