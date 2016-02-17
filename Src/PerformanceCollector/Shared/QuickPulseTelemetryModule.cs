namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    using Timer = Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.Timer.Timer;

    /// <summary>
    /// Telemetry module for collecting QuickPulse data.
    /// </summary>
    public sealed class QuickPulseTelemetryModule : ITelemetryModule, IDisposable
    {
        private const int MaxSampleStorageSize = 10;

        private readonly TimeSpan catastrophicFailureTimeout = TimeSpan.FromSeconds(5);

        private readonly TimeSpan initialDelay = TimeSpan.Zero;

        private readonly object lockObject = new object();

        private readonly object collectedSamplesLock = new object();
        
        private readonly Uri serviceUriDefault = new Uri("https://qps.com/api");

        private readonly LinkedList<QuickPulseDataSample> collectedSamples = new LinkedList<QuickPulseDataSample>();

        private string instrumentationKey;

        private IQuickPulseServiceClient serviceClient;

        private Timer collectionTimer;

        private Timer stateTimer;

        private QuickPulseTimeProvider timeProvider;

        private QuickPulseTimings timings;

        private bool isInitialized;

        private QuickPulseCollectionTimeSlotManager collectionTimeSlotManager = null;

        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

        private IQuickPulseTelemetryProcessor telemetryProcessor = null;

        private QuickPulseCollectionStateManager stateManager = null;

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
        /// <param name="collectionTimeSlotManager">Collection time slot manager.</param>
        /// <param name="dataAccumulatorManager">Data hub to sink QuickPulse data to.</param>
        /// <param name="telemetryProcessor">Telemetry initializer to inspect telemetry stream.</param>
        /// <param name="serviceClient">QPS service client.</param>
        /// <param name="performanceCollector">Performance counter collector.</param>
        /// <param name="timings">Timings for the module.</param>
        internal QuickPulseTelemetryModule(
            QuickPulseCollectionTimeSlotManager collectionTimeSlotManager,
            QuickPulseDataAccumulatorManager dataAccumulatorManager,
            IQuickPulseTelemetryProcessor telemetryProcessor,
            IQuickPulseServiceClient serviceClient,
            IPerformanceCollector performanceCollector,
            QuickPulseTimings timings)
            : this()
        {
            this.collectionTimeSlotManager = collectionTimeSlotManager;
            this.dataAccumulatorManager = dataAccumulatorManager;
            this.telemetryProcessor = telemetryProcessor;
            this.serviceClient = serviceClient;
            this.performanceCollector = performanceCollector;
            this.timings = timings;
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

                        this.collectionTimeSlotManager = this.collectionTimeSlotManager ?? new QuickPulseCollectionTimeSlotManager();
                        this.dataAccumulatorManager = this.dataAccumulatorManager ?? new QuickPulseDataAccumulatorManager();
                        this.performanceCollector = this.performanceCollector ?? new PerformanceCollector();
                        this.timeProvider = this.timeProvider ?? new QuickPulseTimeProvider();
                        this.timings = timings ?? QuickPulseTimings.Default;

                        this.InitializeServiceClient(configuration);

                        this.InitializeTelemetryProcessor(configuration);

                        this.stateManager = new QuickPulseCollectionStateManager(
                            this.serviceClient,
                            this.timeProvider,
                            this.timings,
                            this.OnStartCollection,
                            this.OnStopCollection,
                            this.OnSubmitSamples,
                            this.OnReturnFailedSamples);

                        this.InitializePerformanceCollector();

                        this.InitializeTimers();

                        this.isInitialized = true;
                    }
                }
            }
        }

        private void InitializeTelemetryProcessor(TelemetryConfiguration configuration)
        {
            this.telemetryProcessor = this.telemetryProcessor ?? this.FetchTelemetryProcessor(configuration);

            if (this.telemetryProcessor == null)
            {
                QuickPulseEventSource.Log.CouldNotObtainQuickPulseTelemetryProcessorEvent();

                throw new ArgumentException("Could not obtain an IQuickPulseTelemetryProcessor");
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

        private void InitializeTimers()
        {
            this.collectionTimer = new Timer(this.CollectionTimerCallback);

            this.stateTimer = new Timer(this.StateTimerCallback);
            this.stateTimer.ScheduleNextTick(this.initialDelay);
        }

        private IQuickPulseTelemetryProcessor FetchTelemetryProcessor(TelemetryConfiguration configuration)
        {
            return configuration.TelemetryProcessors.OfType<IQuickPulseTelemetryProcessor>().SingleOrDefault();
        }

        private void InitializeServiceClient(TelemetryConfiguration configuration)
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
            this.serviceClient = new QuickPulseServiceClient(serviceEndpointUri, GetInstanceName(configuration), SdkVersionUtils.GetAssemblyVersion());
        }

        private static string GetInstanceName(TelemetryConfiguration configuration)
        {
            // we need to initialize an item to get instance information
            var fakeItem = new MetricTelemetry();

            try
            {
                new TelemetryClient(configuration).Initialize(fakeItem);
            }
            catch (Exception)
            {
                // we don't care what happened there
            }

            return string.IsNullOrWhiteSpace(fakeItem.Context?.Cloud?.RoleInstance) ? Environment.MachineName : fakeItem.Context.Cloud.RoleInstance;
        }
        
        private void StateTimerCallback(object state)
        {
            var currentCallbackStarted = this.timeProvider.UtcNow;
            TimeSpan? timeToNextUpdate = null;

            try
            {
                // the first instrumentation key that we get from TelemetryConfiguration.Active
                // will be our permanent instrumentation key
                if (string.IsNullOrWhiteSpace(this.instrumentationKey))
                {
                    this.instrumentationKey = TelemetryConfiguration.Active.InstrumentationKey;
                }
                
                timeToNextUpdate = this.stateManager.UpdateState(this.instrumentationKey);
            }
            catch (Exception e)
            {
                QuickPulseEventSource.Log.UnknownErrorEvent(e.ToString());
            }
            finally
            {
                if (this.stateTimer != null)
                {
                    // the 5 second fallback is for the case when we've catastrophically failed some place above
                    timeToNextUpdate = timeToNextUpdate ?? this.catastrophicFailureTimeout;

                    // try to factor in the time spend in this tick when scheduling the next one so that the average period is close to the intended
                    TimeSpan timeSpentInThisCallback = this.timeProvider.UtcNow - currentCallbackStarted;

                    TimeSpan timeLeftUntilNextCallback = timeToNextUpdate.Value - timeSpentInThisCallback;
                    
                    timeLeftUntilNextCallback = timeLeftUntilNextCallback > TimeSpan.Zero
                                                              ? timeLeftUntilNextCallback
                                                              : TimeSpan.Zero;

                    this.stateTimer.ScheduleNextTick(timeLeftUntilNextCallback);
                }
            }
        }

        private void CollectionTimerCallback(object state)
        {
            try
            {
                this.CollectData();
            }
            catch (Exception e)
            {
                QuickPulseEventSource.Log.UnknownErrorEvent(e.ToString());
            }
            finally
            {
                // this is in a race condition with stopping timer from OnStopCollection, so we need to ensure that we don't schedule the next tick more than once
                // after the timer has been ordered to stop
                if (this.stateManager.IsCollectingData && this.collectionTimer != null)
                {
                    DateTime scheduledTime = this.collectionTimeSlotManager.GetNextCollectionTimeSlot(this.timeProvider.UtcNow);
                    this.collectionTimer.ScheduleNextTick(scheduledTime - this.timeProvider.UtcNow);
                }
            }
        }
        
        private void CollectData()
        {
            var sample = this.CollectSample();

            this.StoreSample(sample);
        }

        private void StoreSample(QuickPulseDataSample sample)
        {
            lock (this.collectedSamplesLock)
            {
                this.collectedSamples.AddLast(sample);

                while (this.collectedSamples.Count > MaxSampleStorageSize)
                {
                    this.collectedSamples.RemoveFirst();
                }
            }
        }

        private QuickPulseDataSample CollectSample()
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
            return new QuickPulseDataSample(accumulator, perfData.ToDictionary(tuple => tuple.Item1.ReportAs, tuple => tuple));
        }

        #region Callbacks from the state manager
        private void OnStartCollection()
        {
            this.dataAccumulatorManager.CompleteCurrentDataAccumulator();
            this.telemetryProcessor.StartCollection(this.dataAccumulatorManager);

            DateTime scheduledTime = this.collectionTimeSlotManager.GetNextCollectionTimeSlot(this.timeProvider.UtcNow);
            this.collectionTimer.ScheduleNextTick(scheduledTime - this.timeProvider.UtcNow);
        }
        
        private void OnStopCollection()
        {
            this.collectionTimer.Stop();

            this.telemetryProcessor.StopCollection();

            lock (this.collectedSamplesLock)
            {
                this.collectedSamples.Clear();
            }
        }

        private IList<QuickPulseDataSample> OnSubmitSamples()
        {
            IList<QuickPulseDataSample> samples;

            lock (this.collectedSamplesLock)
            {
                samples = this.collectedSamples.ToList();

                this.collectedSamples.Clear();
            }

            return samples;
        }

        private void OnReturnFailedSamples(IList<QuickPulseDataSample> samples)
        {
            // append the samples that failed to get sent out back to the beginning of the list
            // these will be pushed out as newer samples arrive, so we'll never get more than a certain number
            // even if the network is lagging behind 
            lock (this.collectedSamplesLock)
            {
                foreach (var sample in samples)
                {
                    this.collectedSamples.AddFirst(sample);
                }
            }
        }
        #endregion

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.stateTimer != null)
                {
                    this.stateTimer.Dispose();
                    this.stateTimer = null;
                }

                if (this.collectionTimer != null)
                {
                    this.collectionTimer.Dispose();
                    this.collectionTimer = null;
                }
            }
        }
    }
}