namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implementation of health heartbeat functionality.
    /// </summary>
    internal class HeartbeatProvider : IDisposable, IHeartbeatProvider
    {
        /// <summary>
        /// The default interval between heartbeats if not specified by the user. Left public for use in unit tests.
        /// </summary>
        public static int DefaultHeartbeatIntervalMs = 5000;

        protected List<string> disabledDefaultFields; // string containing fields that are not to be sent with the payload. null means send everything available.

        /// <summary>
        /// The name of the health heartbeat metric item and operation context. 
        /// </summary>
        private static string heartbeatSyntheticMetricName = "HealthState";

        /// <summary>
        /// The SDK supplied 'default' payload items to send out with each health heartbeat.
        /// </summary>
        private ConcurrentDictionary<string, HeartbeatPropertyPayload> sdkPayloadItems;

        /// <summary>
        /// The extended payload items to send out with each health heartbeat.
        /// </summary>
        private ConcurrentDictionary<string, HeartbeatPropertyPayload> extendPayloadItems;

        private bool disposedValue = false; // To detect redundant calls to dispose
        private TimeSpan heartbeatInterval; // time between heartbeats emitted specified in milliseconds
        private TelemetryClient telemetryClient; // client to use in sending our heartbeat
        private int heartbeatsSent; // counter of all heartbeats

        public HeartbeatProvider() : this(TimeSpan.FromMilliseconds(DefaultHeartbeatIntervalMs), null)
        {
        }

        public HeartbeatProvider(TimeSpan heartbeatInterval, IEnumerable<string> disabledDefaultFields)
        {
            this.disabledDefaultFields = disabledDefaultFields?.ToList();
            this.heartbeatInterval = heartbeatInterval;
            this.extendPayloadItems = new ConcurrentDictionary<string, HeartbeatPropertyPayload>(StringComparer.OrdinalIgnoreCase);
            this.sdkPayloadItems = new ConcurrentDictionary<string, HeartbeatPropertyPayload>(StringComparer.OrdinalIgnoreCase);
            this.heartbeatsSent = 0; // count up from construction time
        }

        /// <summary>
        /// Gets the currently defined interval between heartbeats (primarily used in unit testing scenarios)
        /// </summary>
        public TimeSpan HeartbeatInterval => this.heartbeatInterval;

        public string DiagnosticsInstrumentationKey { get; set; }

        protected Timer HeartbeatTimer { get; set; } // timer that will send each heartbeat in intervals

        public virtual bool Initialize(TelemetryConfiguration configuration, string instrumentationKey = null, TimeSpan? timeBetweenHeartbeats = null, IEnumerable<string> disabledDefaultFields = null)
        {
            if (timeBetweenHeartbeats != null && timeBetweenHeartbeats?.TotalMilliseconds == 0)
            {
                return false;
            }

            if (instrumentationKey != null)
            {
                this.DiagnosticsInstrumentationKey = instrumentationKey;
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

            this.SetDefaultPayloadItems();

            // Note: if this is a subsequent initialization, the interval between heartbeats will be updated in the next cycle so no .Change call necessary here
            if (this.HeartbeatTimer == null)
            {
                this.HeartbeatTimer = new Timer(this.HeartbeatPulse, this, this.heartbeatInterval, this.heartbeatInterval);
            }

            return true;
        }

        public bool AddHealthProperty(string name, string value, bool isHealthy)
        {
            if (name != null && 
                !string.IsNullOrEmpty(name) && 
                !HeartbeatDefaultPayload.DefaultFields.Any(key => key.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    return this.AddHealthPropertyInternal(this.extendPayloadItems, name, value, isHealthy);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError(
                        string.Format(CultureInfo.CurrentCulture,
                        "Failed to set a health heartbeat property named '{0}'. Exception: {1}",
                            name,
                            e.ToInvariantString()));
                }
            }

            return false;
        }

        public bool SetHealthProperty(string name, string value = null, bool? isHealthy = null)
        {
            if (!string.IsNullOrEmpty(name))
            {
                try
                {
                    return this.SetHealthPropertyInternal(this.extendPayloadItems, name, value, isHealthy);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError(
                        string.Format(CultureInfo.CurrentCulture,
                        "Failed to set a health heartbeat property named '{0}'. Exception: {1}",
                            name,
                            e.ToInvariantString()));
                }
            }

            return false;
        }

        public bool RemoveHealthProperty(string payloadItemName)
        {
            if (!string.IsNullOrEmpty(payloadItemName))
            {
                try
                {
                    return this.extendPayloadItems.TryRemove(payloadItemName, out HeartbeatPropertyPayload removedItem);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError(
                        string.Format(CultureInfo.CurrentCulture,
                        "Failed to remove a health heartbeat property named '{0}'. Exception: {1}",
                            payloadItemName,
                            e.ToInvariantString()));
                }
            }

            return false;
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

            this.AddPropertiesToHeartbeat(heartbeat, this.sdkPayloadItems);
            this.AddPropertiesToHeartbeat(heartbeat, this.extendPayloadItems);

            heartbeat.Sequence = string.Format(CultureInfo.CurrentCulture, "{0}", this.heartbeatsSent++);

            return heartbeat;
        }

        protected void SetDefaultPayloadItems()
        {
            HeartbeatDefaultPayload defaultPayload = new HeartbeatDefaultPayload(this.disabledDefaultFields);
            IDictionary<string, HeartbeatPropertyPayload> defaultProps = defaultPayload.GetPayloadProperties();

            this.sdkPayloadItems.Clear();
            foreach (var kvpProp in defaultProps)
            {
                this.AddHealthPropertyInternal(this.sdkPayloadItems, kvpProp.Key, kvpProp.Value.PayloadValue, kvpProp.Value.IsHealthy);
            }                
        }

        private void AddPropertiesToHeartbeat(MetricTelemetry hbeat, ConcurrentDictionary<string, HeartbeatPropertyPayload> props)
        {
            string updatedKeys = string.Empty;
            string comma = hbeat.Properties.Count <= 0 ? string.Empty : ",";

            lock (props)
            {
                foreach (var payloadItem in props)
                {
                    try
                    {
                        hbeat.Properties.Add(payloadItem.Key, payloadItem.Value.PayloadValue);
                        hbeat.Sum += payloadItem.Value.IsHealthy ? 0 : 1;
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

            // update the special 'updated keys' property with the names of keys that have been updated.
            if (!string.IsNullOrEmpty(updatedKeys))
            {
                hbeat.Properties[HeartbeatDefaultPayload.UpdatedFieldsPropertyKey] = updatedKeys;
            }
        }

        private bool SetHealthPropertyInternal(ConcurrentDictionary<string, HeartbeatPropertyPayload> properties, string name, string payloadValue, bool? isHealthy)
        {
            try
            {
                properties.AddOrUpdate(name, (key) => 
                {
                    throw new Exception("Not allowed to set this!");
                }, 
                (key, property) =>
                {
                    if (isHealthy != null)
                    {
                        property.IsHealthy = isHealthy.Value;
                    }
                    if (payloadValue != null)
                    {
                        property.PayloadValue = payloadValue;
                    }
                    
                    return property;
                });
            }
            catch (Exception)
            {
                // swallow the exception and return false.
                return false;
            }

            return true;
        }

        private bool AddHealthPropertyInternal(ConcurrentDictionary<string, HeartbeatPropertyPayload> properties, string name, string payloadValue, bool isHealthy)
        {
            bool isAdded = false;
            var existingProp = properties.GetOrAdd(name, (key) =>
            {
                isAdded = true;
                return new HeartbeatPropertyPayload()
                {
                    IsHealthy = isHealthy,
                    PayloadValue = payloadValue
                };
            });

            return isAdded;
        }

        private void HeartbeatPulse(object state)
        {
            if (state is HeartbeatProvider)
            {
                HeartbeatProvider hp = state as HeartbeatProvider;
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

            if (!string.IsNullOrEmpty(this.DiagnosticsInstrumentationKey))
            {
                eventData.Context.InstrumentationKey = this.DiagnosticsInstrumentationKey;
            }

            this.telemetryClient.TrackMetric(eventData);
        }
    }
}