namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    using Timer = Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.Timer.Timer;

    /// <summary>
    /// Telemetry module for collecting QuickPulse data.
    /// </summary>
    public sealed class QuickPulseModule : ITelemetryModule, IDisposable
    {
        private readonly object lockObject = new object();

        private Timer timer = null;

        private bool isInitialized = false;
        
        private QuickPulseDataHub dataHub = null;
        private IQuickPulseTelemetryInitializer telemetryInitializer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseModule"/> class.
        /// </summary>
        public QuickPulseModule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseModule"/> class. Internal constructor for unit tests only.
        /// </summary>
        /// <param name="dataHub">Data hub to sink QuickPulse data to.</param>
        /// <param name="telemetryInitializer">Telemetry initializer to inspect telemetry stream.</param>
        internal QuickPulseModule(QuickPulseDataHub dataHub, IQuickPulseTelemetryInitializer telemetryInitializer) : this()
        {
            this.dataHub = dataHub;
            this.telemetryInitializer = telemetryInitializer;
        }

        /// <summary>
        /// Disposes resources allocated by this type.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize method is called after all configuration properties have been loaded from the configuration.
        /// </summary>
        /// <param name="configuration">TelemetryConfiguration passed to the module.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        QuickPulseEventSource.Log.ModuleIsBeingInitializedEvent(string.Empty);

                        if (configuration == null)
                        {
                            throw new ArgumentNullException(nameof(configuration));
                        }

                        this.dataHub = this.dataHub ?? QuickPulseDataHub.Instance;
                        this.telemetryInitializer = this.telemetryInitializer ??
                                                    new QuickPulseTelemetryInitializer(this.dataHub);

                        if (!configuration.TelemetryInitializers.Any(ti => ti is IQuickPulseTelemetryInitializer))
                        {
                            configuration.TelemetryInitializers.Add(this.telemetryInitializer);
                        }
                        
                        this.timer = new Timer(this.TimerCallback);

                        // this.timer.ScheduleNextTick(this.collectionPeriod);
                        this.isInitialized = true;
                    }
                }
            }
        }

        private void TimerCallback(object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.timer != null)
                {
                    this.timer.Dispose();
                    this.timer = null;
                }
            }
        }
    }
}