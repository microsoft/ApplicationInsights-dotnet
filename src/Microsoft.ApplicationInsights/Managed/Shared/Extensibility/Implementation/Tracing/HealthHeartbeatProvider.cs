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
        private Dictionary<string, HealthHeartbeatPropertyPayload> payloadItems;

        private HealthHeartbeatDefaultPayload defaultPayload; // default items to add to the payload (minus any discluded items)
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
            this.defaultPayload = new HealthHeartbeatDefaultPayload(disabledDefaultFields);
            this.disabledDefaultFields = disabledDefaultFields?.ToList();
            this.heartbeatInterval = heartbeatInterval;
            this.payloadItems = new Dictionary<string, HealthHeartbeatPropertyPayload>(StringComparer.OrdinalIgnoreCase);
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

            if (disabledDefaultFields != null)
            {
                this.disabledDefaultFields = disabledDefaultFields.Count() > 0 ? disabledDefaultFields.ToList() : null;
            }

            this.SetDefaultPayloadItems(this.payloadItems);

            // Note: if this is a subsequent initialization, the interval between heartbeats will be updated in the next cycle so no .Change call necessary here
            if (this.HeartbeatTimer == null)
            {
                this.HeartbeatTimer = new Timer(this.HeartbeatPulse, this, this.heartbeatInterval, this.heartbeatInterval);
            }

            return true;
        }

        public bool SetHealthProperty(HealthHeartbeatProperty payloadItem)
        {
            if (payloadItem != null && !string.IsNullOrEmpty(payloadItem.Name))
            {
                using (var portalSenderLock = new ThreadResourceLock())
                {
                    try
                    {
                        this.SetHealthPropertyInternal(payloadItem.Name, payloadItem.Value.ToString(), payloadItem.IsHealthy);
                        return true;
                    }
                    catch (Exception e)
                    {
                        CoreEventSource.Log.LogError(
                            string.Format(CultureInfo.CurrentCulture,
                            "Failed to set a health heartbeat property named '{0}'. Exception: {1}",
                                payloadItem.Name,
                                e.ToInvariantString()));
                    }
                }
            }

            return false;
        }

        public void UnregisterHeartbeatPayload(string providerName)
        {
            if (this.payloadItems != null)
            {
                this.payloadItems.Remove(providerName);
            }
        }

        #region IDisposable Support

        public void Dispose()
        {
            this.Dispose(true);
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

                this.disposedValue = true;
            }
        }

        #endregion

        protected virtual void Send()
        {
            try
            {
                this.InternalSendHealthHeartbeat(this.GatherData());
            }
            catch (Exception exp)
            {
                // This message will not be sent to the portal because we have infinite loop protection
                // But it will be available in PerfView or StatusMonitor
                CoreEventSource.Log.LogError("Failed to send health heartbeat to the portal: " + exp.ToInvariantString());
            }
        }

        protected virtual MetricTelemetry GatherData()
        {
            var heartbeat = new MetricTelemetry(heartbeatSyntheticMetricName, 0.0);
            string updatedKeys = string.Empty;
            string comma = string.Empty;

            if (!ThreadResourceLock.IsResourceLocked)
            {
                using (var payloadLock = new ThreadResourceLock())
                {
                    foreach (var payloadItem in this.payloadItems)
                    {
                        try
                        {
                            heartbeat.Properties.Add(payloadItem.Key, payloadItem.Value.PayloadValue);
                            heartbeat.Sum += payloadItem.Value.IsHealthy ? 0 : 1;
                            if (payloadItem.Value.IsUpdated)
                            {
                                string.Concat(updatedKeys, comma, payloadItem.Key);
                                comma = ",";
                            }
                        }
                        catch (Exception)
                        {
                            // skip sending this payload item out, no need to interrupt other payloads. Log it to core.
                            CoreEventSource.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to send payload for item {0}.", payloadItem.Key));
                        }
                    }
                }
            }

            // update the special 'updated keys' property with the names of keys that have been updated.
            if (!string.IsNullOrEmpty(updatedKeys))
            {
                heartbeat.Properties[HealthHeartbeatDefaultPayload.UpdatedFieldsPropertyKey] = updatedKeys;
            }

            heartbeat.Sequence = string.Format(CultureInfo.CurrentCulture, "{0}", this.heartbeatsSent++);

            return heartbeat;
        }

        protected void SetDefaultPayloadItems(IDictionary<string, HealthHeartbeatPropertyPayload> heartbeatPayloadItems)
        {
            IDictionary<string, HealthHeartbeatPropertyPayload> defaultProps = this.defaultPayload.GetPayloadProperties();

            using (new ThreadResourceLock())
            {
                // in case we are changing the disabled fields due to re-initialization, remove all defaults and start over
                foreach (string fieldName in HealthHeartbeatDefaultPayload.DefaultFields)
                {
                    this.payloadItems.Remove(fieldName);
                }

                foreach (var kvpProp in defaultProps)
                {
                    this.SetHealthPropertyInternal(kvpProp.Key, kvpProp.Value.PayloadValue, kvpProp.Value.IsHealthy);
                }                
            }
        }

        private void SetHealthPropertyInternal(string name, string payloadValue, bool isHealthy)
        {
            if (this.payloadItems.ContainsKey(name))
            {
                this.payloadItems[name].IsHealthy = isHealthy;
                this.payloadItems[name].PayloadValue = payloadValue;
            }
            else
            {
                this.payloadItems.Add(name, new HealthHeartbeatPropertyPayload()
                {
                    IsHealthy = isHealthy,
                    PayloadValue = payloadValue
                });
            }
        }

        private void HeartbeatPulse(object state)
        {
            if (state is HealthHeartbeatProvider)
            {
                HealthHeartbeatProvider hp = state as HealthHeartbeatProvider;
                // we will be prone to overlap if any extension payload provider takes a longer time to process than our timer
                // interval. Best that we reset the timer each time round.
                this.HeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                hp.Send();
                this.HeartbeatTimer.Change(this.heartbeatInterval, this.heartbeatInterval);
            }
            else
            {
                CoreEventSource.Log.LogError("Heartbeat pulse being sent without valid instance of HealthHeartbeatProvider as its state");
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