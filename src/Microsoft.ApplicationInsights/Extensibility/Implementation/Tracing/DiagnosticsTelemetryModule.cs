namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Use diagnostics telemetry module to report SDK internal problems to the portal and VS debug output window.
    /// </summary>
    public sealed class DiagnosticsTelemetryModule : ITelemetryModule, IDisposable
    {
        internal readonly IList<IDiagnosticsSender> Senders = new List<IDiagnosticsSender>();

        internal readonly DiagnosticsListener EventListener;

        internal IHeartbeatProvider HeartbeatProvider = null;
        private readonly object lockObject = new object();
        private readonly IDiagnoisticsEventThrottlingScheduler throttlingScheduler = new DiagnoisticsEventThrottlingScheduler();
        private volatile bool disposed = false;
        private string instrumentationKey;
        private bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsTelemetryModule"/> class. 
        /// </summary>
        public DiagnosticsTelemetryModule()
        {
            // Adding a dummy queue sender to keep the data to be sent to the portal before the initialize method is called
            this.Senders.Add(new PortalDiagnosticsQueueSender());

            this.EventListener = new DiagnosticsListener(this.Senders);

            this.HeartbeatInterval = TimeSpan.FromMilliseconds(HealthHeartbeatProvider.DefaultHeartbeatIntervalMs);
            this.ExcludedHeartbeatProperties = null;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DiagnosticsTelemetryModule" /> class.
        /// </summary>
        ~DiagnosticsTelemetryModule()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the delay between heartbeats.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; }

        /// <summary>
        /// Gets or sets property names that are not to be sent with the health heartbeats. null/empty list means allow all default properties through.
        /// <remarks>
        /// TODO: this comment should list known properties
        /// </remarks>
        /// </summary>
        public IEnumerable<string> ExcludedHeartbeatProperties { get; set; }

        /// <summary>
        /// Gets or sets diagnostics Telemetry Module LogLevel configuration setting. 
        /// Possible values LogAlways, Critical, Error, Warning, Informational and Verbose.
        /// </summary>
        public string Severity
        {
            get
            {
                return this.EventListener.LogLevel.ToString();
            }

            set
            {
                // Once logLevel is set from configuration, restart listener with new value
                if (!string.IsNullOrEmpty(value))
                {
                    EventLevel parsedValue;
                    if (Enum.IsDefined(typeof(EventLevel), value) == true)
                    {
                        parsedValue = (EventLevel)Enum.Parse(typeof(EventLevel), value, true);
                        this.EventListener.LogLevel = parsedValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets instrumentation key for diagnostics. Use to redirect SDK 
        /// internal problems reporting to the separate instrumentation key.
        /// </summary>
        public string DiagnosticsInstrumentationKey
        {
            get
            {
                return this.instrumentationKey;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.instrumentationKey = value;

                    // Set instrumentation key in Portal sender
                    foreach (var portalSender in this.Senders.OfType<PortalDiagnosticsSender>())
                    {
                        portalSender.DiagnosticsInstrumentationKey = this.instrumentationKey;
                    }

                    // set it into the heartbeat provider as well
                    if (this.HeartbeatProvider != null)
                    {
                        this.HeartbeatProvider.DiagnosticsInstrumentationKey = this.instrumentationKey;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes this telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry configuration to use for this telemetry module.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            // Temporary fix to make sure that we initialize module once.
            // It should be removed when configuration reading logic is moved to Web SDK.
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        var queueSender = this.Senders.OfType<PortalDiagnosticsQueueSender>().First();
                        queueSender.IsDisabled = true;
                        this.Senders.Remove(queueSender);
                        
                        PortalDiagnosticsSender portalSender = new PortalDiagnosticsSender(
                            configuration,
                            new DiagnoisticsEventThrottlingManager<DiagnoisticsEventThrottling>(
                                new DiagnoisticsEventThrottling(DiagnoisticsEventThrottlingDefaults.DefaultThrottleAfterCount),
                                this.throttlingScheduler,
                                DiagnoisticsEventThrottlingDefaults.DefaultThrottlingRecycleIntervalInMinutes));
                        portalSender.DiagnosticsInstrumentationKey = this.DiagnosticsInstrumentationKey;

                        this.Senders.Add(portalSender);

                        foreach (TraceEvent traceEvent in queueSender.EventData)
                        {
                            portalSender.Send(traceEvent);
                        }

                        // set up heartbeat
                        if (this.HeartbeatProvider == null)
                        {
                            this.HeartbeatProvider = new HealthHeartbeatProvider();
                        }

                        this.HeartbeatProvider.Initialize(configuration, this.DiagnosticsInstrumentationKey, this.HeartbeatInterval, this.ExcludedHeartbeatProperties);

                        this.isInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Add a new Health Heartbeat property to the payload sent with each heartbeat.
        /// 
        /// To update the value of the property you are adding to the health heartbeat, 
        /// <see cref="DiagnosticsTelemetryModule.SetHealthProperty"/>.
        /// 
        /// Note that you cannot add a HealthHeartbeatProperty with a name that already exists in the HealthHeartbeat
        /// payload, including (but not limited to) the name of SDK-default items.
        /// 
        /// </summary>
        /// <param name="name">Name of the health heartbeat value to add</param>
        /// <param name="value">Current value of the health heartbeat value to add</param>
        /// <param name="isHealthy">Flag indicating whether or not the property represents a healthy value</param>
        /// <returns>True if the new payload item is successfully added, false otherwise.</returns>
        public bool AddHealthProperty(string name, string value, bool isHealthy)
        {
            if (this.HeartbeatProvider != null)
            {
                try
                {
                    return this.HeartbeatProvider.AddHealthProperty(name, value, isHealthy);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError("Could not add heartbeat property. Exception: " + e.ToInvariantString());
                }
            }

            return false;
        }

        /// <summary>
        /// Set an updated value into an existing property of the health heartbeat.
        /// 
        /// After the new HealthHeartbeatProperty has been added (<see cref="DiagnosticsTelemetryModule.AddHealthProperty"/>) to the 
        /// heartbeat payload, the value represented by that item can be updated using this method at any time.
        /// 
        /// </summary>
        /// <param name="name">Name of the health heartbeat payload item property to set the value and/or health status of.</param>
        /// <param name="value">Value of the health heartbeat payload item. If this is null, the current value in the item is left unchanged.</param>
        /// <param name="isHealthy">Health status of the health heartbeat payload item. If this is set to null the health status is left unchanged.</param>
        /// <returns>True if the payload provider was added, false if it hasn't been added yet 
        /// (<see cref="DiagnosticsTelemetryModule.AddHealthProperty"/>).</returns>
        public bool SetHealthProperty(string name, string value = null, bool? isHealthy = null)
        {
            if (this.HeartbeatProvider != null && !string.IsNullOrEmpty(name) && (value != null || isHealthy != null))
            {
                try
                {
                    return this.HeartbeatProvider.SetHealthProperty(name, value, isHealthy);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError("Could not set heartbeat property. Exception: " + e.ToInvariantString());
                }
            }

            return false;
        }

        /// <summary>
        /// Remove a payload property from the health heartbeat.
        /// 
        /// After the new heartbeat property has been removed, the property will no longer be sent with subsequent heartbeats.
        /// </summary>
        /// <param name="payloadItemName">The name of the property to remove</param>
        /// <returns>True if the property was removed successfully, false otherwise (no property with this name exists)</returns>
        public bool RemoveHealthProperty(string payloadItemName)
        {
            if (this.HeartbeatProvider != null)
            {
                try
                {
                    return this.HeartbeatProvider.RemoveHealthProperty(payloadItemName);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError("Could not set heartbeat property. Exception: " + e.ToInvariantString());
                }
            }

            return false;
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="managed">Indicates if managed code is being disposed.</param>
        private void Dispose(bool managed)
        {
            if (managed && !this.disposed)
            {
                this.EventListener.Dispose();
                (this.throttlingScheduler as IDisposable).Dispose();
                foreach (var disposableSender in this.Senders.OfType<IDisposable>())
                {
                    disposableSender.Dispose();
                }

                this.HeartbeatProvider = null;

                GC.SuppressFinalize(this);
            }

            this.disposed = true;
        }
    }
}
