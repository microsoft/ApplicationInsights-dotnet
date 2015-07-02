namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
#if CORE_PCL || NET45 || WINRT
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;

#if NET40 || NET35
    using Microsoft.Diagnostics.Tracing;
#endif

    internal class DiagnosticsTelemetryModule : ITelemetryModule, IDisposable
    {
        protected readonly IList<IDiagnosticsSender> Senders = new List<IDiagnosticsSender>();

        protected DiagnosticsListener eventListener;

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

            this.Senders.Add(new F5DiagnosticsSender());
            this.eventListener = new DiagnosticsListener(this.Senders);

#if Wp80
            CoreEventSource.Log.EnableEventListener(this.eventListener);
#endif
        }

        ~DiagnosticsTelemetryModule()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets diagnostics Telemetry Module LogLevel configuration setting.
        /// </summary>
        public string Severity
        {
            get
            {
                return this.eventListener.LogLevel.ToString();
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
                        this.eventListener.LogLevel = parsedValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets instrumentation key for diagnostics.
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

                        this.isInitialized = true;
                    }
                }
            }
        }
        
        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool managed)
        {
            if (true == managed && false == this.disposed)
            {
                this.eventListener.Dispose();
                (this.throttlingScheduler as IDisposable).Dispose();

                GC.SuppressFinalize(this);
            }

            this.disposed = true;
        }
    }
}
