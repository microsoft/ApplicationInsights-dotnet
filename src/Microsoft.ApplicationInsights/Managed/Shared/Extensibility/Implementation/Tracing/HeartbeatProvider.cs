namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
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

        protected UInt64 heartbeatsSent; // counter of all heartbeats

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
        private volatile bool isEnabled; // no need for locks or volatile here, we can skip/add a beat if the module is disabled between heartbeats

        public HeartbeatProvider()
        {
            this.disabledDefaultFields = null;
            this.heartbeatInterval = TimeSpan.FromMilliseconds(DefaultHeartbeatIntervalMs);
            this.extendPayloadItems = new ConcurrentDictionary<string, HeartbeatPropertyPayload>(StringComparer.OrdinalIgnoreCase);
            this.sdkPayloadItems = new ConcurrentDictionary<string, HeartbeatPropertyPayload>(StringComparer.OrdinalIgnoreCase);
            this.heartbeatsSent = 0; // count up from construction time
            this.isEnabled = false; // wait until Initialize is called before this means anything
        }

        /// <summary>
        /// Gets or sets the currently defined interval between heartbeats
        /// </summary>
        public TimeSpan Interval
        {
            get => this.heartbeatInterval;
            set
            {
                if (value == null || value.TotalMilliseconds <= 0)
                {
                    this.heartbeatInterval = TimeSpan.FromMilliseconds(DefaultHeartbeatIntervalMs);
                }
                else
                {
                    this.heartbeatInterval = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently defined instrumentation key to send heartbeat telemetry items to.
        /// 
        /// Note that if the heartbeat provider has not been initialized yet, this key would get reset to 
        /// whatever the telemetry configuration the heartbeat provider is initialized with.
        /// </summary>
        public string InstrumentationKey
        {
            get => this.telemetryClient?.InstrumentationKey;
            set
            {
                if (this.telemetryClient != null)
                {
                    this.telemetryClient.InstrumentationKey = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not heartbeats are enabled
        /// </summary>
        public bool IsEnabled
        {
            get => this.isEnabled;
            set
            {
                if (!this.isEnabled && value)
                {
                    // we need to start calling the timer again
                    this.HeartbeatTimer.Change(this.heartbeatInterval, this.heartbeatInterval);
                }

                this.isEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a list of default field names that should not be sent with each heartbeat.
        /// </summary>
        public IEnumerable<string> DisabledDefaultFields
        {
            get => this.disabledDefaultFields;
            set
            {
                this.disabledDefaultFields = null;
                if (value != null)
                {
                    this.disabledDefaultFields = new List<string>(value);
                }

                this.SetDefaultPayloadItems();
            }
        }

        private Timer HeartbeatTimer { get; set; } // timer that will send each heartbeat in intervals

        public void Initialize(TelemetryConfiguration configuration)
        {
            if (this.telemetryClient == null)
            {
                this.telemetryClient = new TelemetryClient(configuration);
            }

            this.SetDefaultPayloadItems();

            this.isEnabled = true;

            // Note: if this is a subsequent initialization, the interval between heartbeats will be updated in the next cycle so no .Change call necessary here
            if (this.HeartbeatTimer == null)
            {
                this.HeartbeatTimer = new Timer(this.HeartbeatPulse, this, this.heartbeatInterval, this.heartbeatInterval);
            }
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

        /// <summary>
        /// Get the metric telemetry item that will be sent.
        /// 
        /// Note: exposed to internal to allow inspection for testing.
        /// </summary>
        /// <returns>A MetricTelemtry item that contains the currently defined payload for a heartbeat 'pulse'.</returns>
        internal ITelemetry GatherData()
        {
            var heartbeat = new MetricTelemetry(heartbeatSyntheticMetricName, 0.0);

            this.AddPropertiesToHeartbeat(heartbeat, this.sdkPayloadItems);
            this.AddPropertiesToHeartbeat(heartbeat, this.extendPayloadItems);

            heartbeat.Sequence = string.Format(CultureInfo.CurrentCulture, "{0}", this.heartbeatsSent++);

            return heartbeat;
        }

        protected void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.HeartbeatTimer != null)
                    {
                        this.isEnabled = false;

                        try
                        {
                            this.HeartbeatTimer.Dispose();
                        }
                        catch (Exception e)
                        {
                            CoreEventSource.Log.LogError("Disposing heartbeat timer results in an exception: " + e.ToInvariantString());
                        }
                    }
                }

                this.disposedValue = true;
            }
        }

        #endregion

        private void Send()
        {
            if (this.telemetryClient.TelemetryConfiguration.TelemetryChannel == null)
            {
                return;
            }

            var eventData = (MetricTelemetry)this.GatherData();

            eventData.Context.Operation.SyntheticSource = heartbeatSyntheticMetricName;

            eventData.Context.InstrumentationKey = this.InstrumentationKey;

            this.telemetryClient.TrackMetric(eventData);
        }

        private void SetDefaultPayloadItems()
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
            bool setResult = true;

            properties.AddOrUpdate(name, (key) => 
            {
                setResult = false;
                throw new Exception("Not allowed to set a health property without adding it first.");
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

            return setResult;
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
                try
                {
                    this.HeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    if (this.isEnabled)
                    {
                        hp.Send();
                        this.HeartbeatTimer.Change(this.heartbeatInterval, this.heartbeatInterval);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // swallow this uninteresting exception but log it just the same.
                    CoreEventSource.Log.LogError("Heartbeat timer change during dispose occured.");
                }
            }
            else
            {
                CoreEventSource.Log.LogError("Heartbeat pulse being sent without valid instance of HealthHeartbeatProvider as its state");
            }
        }
    }
}
