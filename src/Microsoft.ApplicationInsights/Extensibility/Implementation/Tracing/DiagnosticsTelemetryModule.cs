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

            this.TimeSpanBetweenHeartbeats = TimeSpan.FromMilliseconds(HealthHeartbeatProvider.DefaultHeartbeatIntervalMs);
            this.DisableHeartbeatProperties = null;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DiagnosticsTelemetryModule" /> class.
        /// </summary>
        ~DiagnosticsTelemetryModule()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the delay between heartbeats in milliseconds.
        /// </summary>
        public TimeSpan TimeSpanBetweenHeartbeats { get; set; }

        /// <summary>
        /// Gets or sets property names that are not to be sent with the health heartbeats. null/empty list means allow all default properties through.
        /// <remarks>
        /// TODO: this comment should list known properties
        /// </remarks>
        /// </summary>
        public IEnumerable<string> DisableHeartbeatProperties { get; set; }

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

                        this.HeartbeatProvider.Initialize(configuration, this.TimeSpanBetweenHeartbeats, this.DisableHeartbeatProperties);

                        this.isInitialized = true;
                    }
                }
            }
        }

        public bool AddHealthProperty(HealthHeartbeatProperty payloadItem)
        {
            if (this.HeartbeatProvider != null)
            {
                try
                {
                    return this.HeartbeatProvider.AddHealthProperty(payloadItem);
                }
                catch (Exception e)
                {
                    CoreEventSource.Log.LogError("Could not add heartbeat property. Exception: " + e.ToInvariantString());
                }
            }

            return false;
        }

        /// <summary>
        /// Allows consumers of the DiagosticsTelemetryModule to set an updated value into an existing property of the health heartbeat.
        /// 
        /// Each HealthHeartbeatProperty set into the health heartbeat will be sent with the latest value set into it.
        /// 
        /// </summary>
        /// <param name="payloadItem">Payload to include in Health Heartbeat telemetry item</param>
        /// <returns>True if the payload provider was added, false otherwise.</returns>
        public bool SetHealthProperty(HealthHeartbeatProperty payloadItem)
        {
            if (this.HeartbeatProvider != null)
            {
                try
                {
                    return this.HeartbeatProvider.SetHealthProperty(payloadItem);
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
