namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    using Timer = Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.Timer.Timer;

    /// <summary>
    /// Telemetry module for collecting QuickPulse data.
    /// </summary>
    public sealed class QuickPulseTelemetryModule : ITelemetryModule, IDisposable
    {
        private readonly object lockObject = new object();

        private readonly TimeSpan servicePollingInterval = TimeSpan.FromSeconds(5);

        private readonly TimeSpan collectionInterval = TimeSpan.FromSeconds(1);

        private readonly Uri serviceUriDefault = new Uri("https://qps.com/api");

        private IQuickPulseServiceClient serviceClient;

        private Timer timer = null;

        private bool isInitialized = false;

        private QuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private IQuickPulseTelemetryInitializer telemetryInitializer = null;

        private QuickPulseCollectionStateManager collectionStateManager = null;

        private IPerformanceCollector performanceCollector = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryModule"/> class.
        /// </summary>
        public QuickPulseTelemetryModule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickPulseTelemetryModule"/> class. Internal constructor for unit tests only.
        /// </summary>
        /// <param name="dataAccumulatorManager">Data hub to sink QuickPulse data to.</param>
        /// <param name="telemetryInitializer">Telemetry initializer to inspect telemetry stream.</param>
        /// <param name="serviceClient">QPS service client.</param>
        /// <param name="performanceCollector">Performance counter collector.</param>
        /// <param name="servicePollingInterval">Interval to poll the service at.</param>
        /// <param name="collectionInterval">Interval to collect data at.</param>
        internal QuickPulseTelemetryModule(
            QuickPulseDataAccumulatorManager dataAccumulatorManager,
            IQuickPulseTelemetryInitializer telemetryInitializer,
            IQuickPulseServiceClient serviceClient,
            IPerformanceCollector performanceCollector,
            TimeSpan? servicePollingInterval,
            TimeSpan? collectionInterval)
            : this()
        {
            this.dataAccumulatorManager = dataAccumulatorManager;
            this.telemetryInitializer = telemetryInitializer;
            this.serviceClient = serviceClient;
            this.performanceCollector = performanceCollector;
            this.servicePollingInterval = servicePollingInterval ?? this.servicePollingInterval;
            this.collectionInterval = collectionInterval ?? this.collectionInterval;
        }

        /// <summary>
        /// Gets the QuickPulse service endpoint to send to.
        /// </summary>
        /// <remarks>Loaded from configuration.</remarks>
        public string QuickPulseServiceEndpoint { get; private set; }

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
                        QuickPulseEventSource.Log.ModuleIsBeingInitializedEvent(
                            string.Format(CultureInfo.InvariantCulture, "QuickPulseServiceEndpoint: '{0}'", this.QuickPulseServiceEndpoint));

                        this.ValidateConfiguration(configuration);

                        this.dataAccumulatorManager = this.dataAccumulatorManager ?? new QuickPulseDataAccumulatorManager();
                        this.performanceCollector = this.performanceCollector ?? new PerformanceCollector();

                        this.InitializeServiceClient();

                        this.telemetryInitializer = this.telemetryInitializer ?? new QuickPulseTelemetryInitializer();
                        this.PlugInTelemetryInitializer(configuration);

                        this.collectionStateManager = new QuickPulseCollectionStateManager(
                            this.serviceClient,
                            () =>
                                {
                                    this.dataAccumulatorManager.CompleteCurrentDataAccumulator();
                                    this.telemetryInitializer.StartCollection(this.dataAccumulatorManager);
                                },
                            () => this.telemetryInitializer.StopCollection(),
                            this.CollectData);

                        this.InitializePerformanceCollector();

                        this.StartTimer();

                        this.isInitialized = true;
                    }
                }
            }
        }

        private void InitializePerformanceCollector()
        {
            foreach (var counter in QuickPulsePerfCounterList.CountersToCollect)
            {
                PerformanceCounter pc = null;
                bool usesPlaceholder;

                try
                {
                    pc = PerformanceCounterUtility.ParsePerformanceCounter(counter.Item2, null, null, out usesPlaceholder);
                }
                catch (Exception e)
                {
                    QuickPulseEventSource.Log.CounterParsingFailedEvent(e.Message, counter.Item2);
                    continue;
                }

                if (usesPlaceholder)
                {
                    throw new InvalidOperationException(
                        "Instance placeholders are not currently supported since they require refresh. Refresh is not implemented at this time.");
                }

                try
                {
                    this.performanceCollector.RegisterPerformanceCounter(
                        counter.Item2,
                        counter.Item1.ToString(),
                        pc.CategoryName,
                        pc.CounterName,
                        pc.InstanceName,
                        false,
                        true);

                    QuickPulseEventSource.Log.CounterRegisteredEvent(counter.Item2);
                }
                catch (InvalidOperationException e)
                {
                    QuickPulseEventSource.Log.CounterRegistrationFailedEvent(e.Message, counter.Item2);
                }
            }
        }

        private void ValidateConfiguration(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.TelemetryInitializers == null)
            {
                throw new ArgumentNullException(nameof(configuration.TelemetryInitializers));
            }
        }

        private void StartTimer()
        {
            this.timer = new Timer(this.TimerCallback);

            this.timer.ScheduleNextTick(this.servicePollingInterval);
        }

        private void PlugInTelemetryInitializer(TelemetryConfiguration configuration)
        {
            configuration.TelemetryInitializers.Add(this.telemetryInitializer);
        }

        private void InitializeServiceClient()
        {
            if (this.serviceClient != null)
            {
                // service client has been passed through a constructor, we don't need to do anything
                return;
            }

            Uri serviceEndpointUri;
            if (string.IsNullOrWhiteSpace(this.QuickPulseServiceEndpoint))
            {
                // endpoint is not specified in configuration, use the default one
                serviceEndpointUri = this.serviceUriDefault;
            }
            else
            {
                // endpoint appears to have been specified in configuration, try using it
                try
                {
                    serviceEndpointUri = new Uri(this.QuickPulseServiceEndpoint);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Error initializing QuickPulse module. QPS endpoint is not a correct URI: '{0}'",
                            this.QuickPulseServiceEndpoint),
                        e);
                }
            }

            // create the default production implementation of the service client with the best service endpoint we could get
            this.serviceClient = new QuickPulseServiceClient(serviceEndpointUri);
        }

        private void TimerCallback(object state)
        {
            try
            {
                this.collectionStateManager.PerformAction();
            }
            catch (Exception e)
            {
                QuickPulseEventSource.Log.UnknownErrorEvent(e.ToString());
            }
            finally
            {
                if (this.timer != null)
                {
                    this.timer.ScheduleNextTick(this.collectionStateManager.IsCollectingData ? this.collectionInterval : this.servicePollingInterval);
                }
            }
        }

        /// <summary>
        /// Data collection entry point.
        /// </summary>
        /// <returns><b>true</b> if we need to keep collecting data, <b>false</b> otherwise.</returns>
        private QuickPulseDataSample CollectData()
        {
            // For AI data, all we have to do is lock the current accumulator in
            QuickPulseDataAccumulator completeAccumulator = this.dataAccumulatorManager.CompleteCurrentDataAccumulator();

            // For performance collection, we have to read perf samples from Windows
            List<Tuple<PerformanceCounterData, float>> perfData =
                this.performanceCollector.Collect((counterName, e) => QuickPulseEventSource.Log.CounterReadingFailedEvent(e.ToString(), counterName))
                    .ToList();

            return this.CreateDataSample(completeAccumulator, perfData);
        }

        private QuickPulseDataSample CreateDataSample(
            QuickPulseDataAccumulator accumulator,
            IEnumerable<Tuple<PerformanceCounterData, float>> perfData)
        {
            return new QuickPulseDataSample(accumulator, perfData.ToLookup(tuple => tuple.Item1.ReportAs, tuple => tuple.Item2));
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