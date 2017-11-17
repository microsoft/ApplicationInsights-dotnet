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

        /// <summary>
        /// The name of the health heartbeat metric item and operation context. 
        /// </summary>
        private static string heartbeatSyntheticMetricName = "HealthState";

        private readonly List<string> disabledDefaultFields = new List<string>(); // string containing fields that are not to be sent with the payload. Empty list means send everything available.

        private UInt64 heartbeatsSent; // counter of all heartbeats

        /// <summary>
        /// The payload items to send out with each health heartbeat.
        /// </summary>
        private ConcurrentDictionary<string, HeartbeatPropertyPayload> heartbeatProperties;

        private bool disposedValue = false; // To detect redundant calls to dispose
        private TimeSpan heartbeatInterval; // time between heartbeats emitted specified in milliseconds
        private TelemetryClient telemetryClient; // client to use in sending our heartbeat
        private volatile bool isEnabled; // no need for locks or volatile here, we can skip/add a beat if the module is disabled between heartbeats

        public HeartbeatProvider()
        {
            this.heartbeatInterval = TimeSpan.FromMilliseconds(DefaultHeartbeatIntervalMs);
            this.heartbeatProperties = new ConcurrentDictionary<string, HeartbeatPropertyPayload>(StringComparer.OrdinalIgnoreCase);
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
                    this.HeartbeatTimer.Change(this.Interval, this.Interval);
                }

                this.isEnabled = value;
            }
        }

        /// <summary>
        /// Gets a list of default field names that should not be sent with each heartbeat.
        /// </summary>
        public IList<string> DisabledDefaultFields
        {
            get => this.disabledDefaultFields;
        }

        private Timer HeartbeatTimer { get; set; } // timer that will send each heartbeat in intervals

        public void Initialize(TelemetryConfiguration configuration)
        {
            if (this.telemetryClient == null)
            {
                this.telemetryClient = new TelemetryClient(configuration);
            }

            HeartbeatDefaultPayload.GetPayloadProperties(this.DisabledDefaultFields, this);

            this.isEnabled = true;

            // Note: if this is a subsequent initialization, the interval between heartbeats will be updated in the next cycle so no .Change call necessary here
            if (this.HeartbeatTimer == null)
            {
                this.HeartbeatTimer = new Timer(this.HeartbeatPulse, this, this.Interval, this.Interval);
            }
        }

        public bool AddHealthProperty(string healthPropertyName, string healthPropertyValue, bool isHealthy)
        {
            if (!string.IsNullOrEmpty(healthPropertyName))
            {
                try
                {
                    bool isAdded = false;
                    var existingProp = this.heartbeatProperties.GetOrAdd(healthPropertyName, (key) =>
                    {
                        isAdded = true;
                        return new HeartbeatPropertyPayload()
                        {
                            IsHealthy = isHealthy,
                            PayloadValue = healthPropertyValue
                        };
                    });

                    return isAdded;
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.FailedToAddHeartbeatProperty(healthPropertyName, healthPropertyValue, e.ToString());
                }
            }
            else
            {
                CoreEventSource.Log.FailedToAddHeartbeatProperty(healthPropertyName, healthPropertyValue);
            }

            return false;
        }

        public bool SetHealthProperty(string healthPropertyName, string healthPropertyValue = null, bool? isHealthy = null)
        {
            if (!string.IsNullOrEmpty(healthPropertyName)
                && !HeartbeatDefaultPayload.DefaultFields.Contains(healthPropertyName))
            {
                try
                {
                    bool setResult = true;

                    this.heartbeatProperties.AddOrUpdate(healthPropertyName, (key) =>
                    {
                        setResult = false;
                        throw new Exception("Cannot set a health property without adding it first.");
                    },
                    (key, property) =>
                    {
                        if (isHealthy != null)
                        {
                            property.IsHealthy = isHealthy.Value;
                        }
                        if (healthPropertyValue != null)
                        {
                            property.PayloadValue = healthPropertyValue;
                        }

                        return property;
                    });

                    return setResult;
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.FailedToSetHeartbeatProperty(healthPropertyName, healthPropertyValue, isHealthy.HasValue ? isHealthy.Value.ToString() : "null", e.ToString());
                }
            }
            else
            {
                CoreEventSource.Log.FailedToSetHeartbeatProperty(healthPropertyName, healthPropertyValue, isHealthy.HasValue ? isHealthy.Value.ToString() : "null");
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
            var hbeat = new MetricTelemetry(heartbeatSyntheticMetricName, 0.0);

            string updatedKeys = string.Empty;
            string comma = string.Empty;

            if (hbeat.Properties.ContainsKey(HeartbeatDefaultPayload.UpdatedFieldsPropertyKey))
            {
                updatedKeys = hbeat.Properties[HeartbeatDefaultPayload.UpdatedFieldsPropertyKey];
                comma = ",";
            }

            foreach (var payloadItem in this.heartbeatProperties)
            {
                hbeat.Properties.Add(payloadItem.Key, payloadItem.Value.PayloadValue);
                hbeat.Sum += payloadItem.Value.IsHealthy ? 0 : 1;
                if (payloadItem.Value.IsUpdated)
                {
                    string.Concat(updatedKeys, comma, payloadItem.Key);
                    comma = ",";
                }
            }

            // update the special 'updated keys' property with the names of keys that have been updated.
            if (!string.IsNullOrEmpty(updatedKeys))
            {
                hbeat.Properties[HeartbeatDefaultPayload.UpdatedFieldsPropertyKey] = updatedKeys;
            }

            hbeat.Sequence = string.Format(CultureInfo.CurrentCulture, "{0}", this.heartbeatsSent++);

            return hbeat;
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
                        catch (Exception)
                        {
                            CoreEventSource.Log.LogError("Error occured when disposing heartbeat timer within HeartbeatProvider");
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
                        this.HeartbeatTimer.Change(this.Interval, this.Interval);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // swallow this exception but log it just the same.
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
