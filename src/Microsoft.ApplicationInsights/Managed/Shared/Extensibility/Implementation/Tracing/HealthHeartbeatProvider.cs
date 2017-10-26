namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
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
        /// The name of the health heartbeat metric item and operation context. 
        /// </summary>
        private static string heartbeatSyntheticMetricName = "HealthState";

        /// <summary>
        /// The payload items to send out with each health heartbeat.
        /// </summary>
        private Dictionary<string, IHealthHeartbeatPayloadExtension> payloadItems;
        
        private bool disposedValue = false; // To detect redundant calls to dispose
        private TimeSpan heartbeatInterval; // time between heartbeats emitted specified in milliseconds
        private List<string> disabledDefaultFields; // string containing fields that are not to be sent with the payload. null means send everything available.
        private TelemetryClient telemetryClient; // client to use in sending our heartbeat
        private int heartbeatsSent; // counter of all heartbeats

        public HealthHeartbeatProvider() : this(TimeSpan.FromMilliseconds(DefaultHeartbeatIntervalMs), null)
        {
        }

        public HealthHeartbeatProvider(TimeSpan heartbeatInterval, IEnumerable<string> disabledDefaultFields)
        {
            this.disabledDefaultFields = disabledDefaultFields?.ToList();
            this.heartbeatInterval = heartbeatInterval;
            this.payloadItems = new Dictionary<string, IHealthHeartbeatPayloadExtension>();
            this.heartbeatsSent = 0; // count up from construction time
        }

        public TimeSpan HeartbeatInterval => this.heartbeatInterval;

        public IEnumerable<string> DisabledHeartbeatProperties => this.disabledDefaultFields;

        protected Timer HeartbeatTimer { get; set; } // timer that will send each heartbeat in intervals

        public virtual bool Initialize(TelemetryConfiguration configuration, TimeSpan? timeBetweenHeartbeats = null, IEnumerable<string> disabledDefaultFields = null)
        {
            if (timeBetweenHeartbeats != null && timeBetweenHeartbeats?.TotalMilliseconds == 0)
            {
                return false;
            }

            if (this.telemetryClient == null)
            {
                this.telemetryClient = new TelemetryClient(configuration);
            }

            this.heartbeatInterval = timeBetweenHeartbeats ?? this.heartbeatInterval;

            this.disabledDefaultFields = disabledDefaultFields?.ToList();

            this.AddDefaultPayloadItems(this.payloadItems);

            // Note: if this is a subsequent initialization, the interval between heartbeats will be updated in the next cycle so no .Change call necessary here
            if (this.HeartbeatTimer == null)
            {
                this.HeartbeatTimer = new Timer(this.HeartbeatPulse, this, this.heartbeatInterval, this.heartbeatInterval);
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
                    if (this.HeartbeatTimer != null)
                    {
                        this.HeartbeatTimer.Dispose();
                    }
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                this.disposedValue = true;
            }
        }

        #endregion

        protected virtual void Send()
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

        protected virtual MetricTelemetry GatherData()
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

        protected void AddDefaultPayloadItems(IDictionary<string, IHealthHeartbeatPayloadExtension> heartbeatPayloadItems)
        {
            try
            {
                heartbeatPayloadItems[string.Empty] = new HealthHeartbeatDefaultPayload(this.disabledDefaultFields);
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
                // we are being called from a different thread here. We will need to block access to the list of payload providers
                lock (this.payloadItems)
                {
                    // we will be prone to overlap if any extension payload provider takes a longer time to process than our timer
                    // interval. Best that we reset the timer each time round.
                    this.HeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    hp.Send();
                    this.HeartbeatTimer.Change(this.heartbeatInterval, this.heartbeatInterval);
                }
            }
            else
            {
                throw new ArgumentException("Heartbeat pulse being sent without valid instance of HealthHeartbeatProvider as its state");
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