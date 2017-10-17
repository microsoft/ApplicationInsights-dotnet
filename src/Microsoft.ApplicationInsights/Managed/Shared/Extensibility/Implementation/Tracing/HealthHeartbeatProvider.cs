namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implementation of health heartbeat functionality.
    /// </summary>
    internal class HealthHeartbeatProvider : IDisposable, IHeartbeatProvider
    {
        /// <summary>
        /// The default fields to include in every heartbeat sent. Note that setting the value to '*' includes all default fields.
        /// </summary>
        public static string DefaultAllowedFieldsInHeartbeatPayload = "*";

        /// <summary>
        /// The name of the health heartbeat metric item and operation context.
        /// </summary>
        private static string heartbeatSyntheticMetricName = "SDKHeartbeat";

        /// <summary>
        /// The default interval between heartbeats if not specified by the user
        /// </summary>
        private static int defaultHeartbeatIntervalMs = 5000;

        /// <summary>
        /// The payload items to send out with each health heartbeat.
        /// </summary>
        private Dictionary<string, IHealthHeartbeatPayloadExtension> payloadItems;
        
        private bool disposedValue = false; // To detect redundant calls to dispose
        private int intervalBetweenHeartbeatsMs; // time between heartbeats emitted specified in milliseconds
        private string enabledHeartbeatPayloadFields; // string containing fields that are enabled in the payload. * means everything available.
        private TelemetryClient telemetryClient; // client to use in sending our heartbeat
        private int heartbeatsSent; // counter of all heartbeats
        private Timer heartbeatTimer; // timer that will send each heartbeat in intervals

        public HealthHeartbeatProvider() : this(defaultHeartbeatIntervalMs, DefaultAllowedFieldsInHeartbeatPayload)
        {
        }

        public HealthHeartbeatProvider(int delayMs) : this(delayMs, DefaultAllowedFieldsInHeartbeatPayload)
        {
        }

        public HealthHeartbeatProvider(string allowedPayloadFields) : this(defaultHeartbeatIntervalMs, allowedPayloadFields)
        {
        }

        public HealthHeartbeatProvider(int delayMs, string allowedPayloadFields)
        {
            this.enabledHeartbeatPayloadFields = allowedPayloadFields;
            this.intervalBetweenHeartbeatsMs = delayMs;
            this.payloadItems = new Dictionary<string, IHealthHeartbeatPayloadExtension>();
            this.heartbeatsSent = 0; // count up from construction time
        }

        public int HeartbeatIntervalMs => this.intervalBetweenHeartbeatsMs;

        public string EnabledPayloadFields => this.enabledHeartbeatPayloadFields;

        public bool Initialize(TelemetryConfiguration configuration, int? delayMs = null, string allowedPayloadFields = null)
        {
            if (this.telemetryClient == null)
            {
                this.telemetryClient = new TelemetryClient(configuration);
            }

            this.intervalBetweenHeartbeatsMs = delayMs.GetValueOrDefault(this.intervalBetweenHeartbeatsMs);

            if (!string.IsNullOrEmpty(allowedPayloadFields))
            {
                this.enabledHeartbeatPayloadFields = allowedPayloadFields;
            }

            this.AddDefaultPayloadItems(this.payloadItems);
            if (this.heartbeatTimer == null)
            {
                this.heartbeatTimer = new Timer(this.HeartbeatPulse, this, this.intervalBetweenHeartbeatsMs, this.intervalBetweenHeartbeatsMs);
            }
            else
            {
                this.heartbeatTimer.Change(this.intervalBetweenHeartbeatsMs, this.intervalBetweenHeartbeatsMs);
            }
            
            return true;
        }

        public void RegisterHeartbeatPayload(IHealthHeartbeatPayloadExtension payloadProvider)
        {
            if (payloadProvider == null)
            {
                throw new ArgumentNullException(nameof(payloadProvider));
            }

            if (string.IsNullOrEmpty(payloadProvider.Name) || this.payloadItems.ContainsKey(payloadProvider.Name))
            {
                throw new ArgumentNullException(nameof(payloadProvider), "Name member of IHealthHeartbeatPayloadExtension must be set to a unique value and cannot be empty");
            }

            this.payloadItems.Add(payloadProvider.Name, payloadProvider);
        }

        public void UnregisterHeartbeatPayload(string providerName)
        {
            if (this.payloadItems != null)
            {
                this.payloadItems.Remove(providerName);
            }
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

        protected void AddDefaultPayloadItems(IDictionary<string, IHealthHeartbeatPayloadExtension> heartbeatPayloadItems)
        {
            try
            {
                heartbeatPayloadItems[string.Empty] = new HealthHeartbeatDefaultPayload(this.enabledHeartbeatPayloadFields);
            }
            catch (Exception e)
            {
                throw new ArgumentException("HealthHeartbeatProvider::AddDefaultPayloadItems : Unable to add default payload items to health heartbeat", nameof(heartbeatPayloadItems), e);
            }
        }

        private void HeartbeatPulse(object state)
        {
            if (state is HealthHeartbeatProvider)
            {
                HealthHeartbeatProvider hp = state as HealthHeartbeatProvider;
                hp.Send();
            }
            else
            {
                throw new ArgumentException("Heartbeat pulse being sent without valid instance of HealthHeartbeatProvider as its state");
            }
        }

        private MetricTelemetry GatherDataForHeartbeatPayload()
        {
            var heartbeat = new MetricTelemetry(heartbeatSyntheticMetricName, 0.0);

            foreach (var payloadItem in this.payloadItems)
            {
                try
                {
                    var props = payloadItem.Value.GetPayloadProperties();
                    foreach (var kvp in props)
                    {
                        heartbeat.Properties.Add(kvp.Key, kvp.Value.ToString());
                    }

                    heartbeat.Sum += payloadItem.Value.CurrentUnhealthyCount;
                }
                catch (Exception)
                {
                    // skip sending this payload item out, no need to interrupt other payloads. Log it to core.
                    CoreEventSource.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to send payload for item {0}.", payloadItem.Key));
                }
            }

            heartbeat.Sequence = string.Format(CultureInfo.InvariantCulture, "{0}", this.heartbeatsSent++);

            return heartbeat;
        }

        private void SendHealthHeartbeat()
        {
            try
            {
                var heartbeatPayload = this.GatherData();
                if (heartbeatPayload.Properties != null && heartbeatPayload.Count > 0)
                {
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
                                CoreEventSource.Log.LogError("Failed to send health heartbeat to the portal: " + exp.ToInvariantString());
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // We were trying to send heartbeats out and failed. 
                // No reason to try to send it out again, users can deduce the missing packets via sequence (or that 
                // they simply don't get them any longer)
            }
        }

        private void InternalSendHealthHeartbeat(MetricTelemetry eventData)
        {
            if (this.telemetryClient.TelemetryConfiguration.TelemetryChannel == null)
            {
                return;
            }

            eventData.Context.Operation.SyntheticSource = heartbeatSyntheticMetricName;

            this.telemetryClient.TrackMetric(eventData);
        }
    }
}