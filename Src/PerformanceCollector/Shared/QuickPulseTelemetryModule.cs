namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    /// <summary>
    /// Telemetry module for collecting QuickPulse data.
    /// </summary>
    public sealed class QuickPulseTelemetryModule : ITelemetryModule, IDisposable
    {
        private const int MaxSampleStorageSize = 10;

        private readonly object lockObject = new object();

        private readonly object telemetryProcessorsLock = new object();

        private readonly object collectedSamplesLock = new object();

        private readonly LinkedList<QuickPulseDataSample> collectedSamples = new LinkedList<QuickPulseDataSample>();

        private readonly LinkedList<IQuickPulseTelemetryProcessor> telemetryProcessors = new LinkedList<IQuickPulseTelemetryProcessor>();

        private TelemetryConfiguration config;

        private IQuickPulseServiceClient serviceClient;

        private Thread collectionThread;

        private QuickPulseThreadState collectionThreadState;

        private Thread stateThread;

        private QuickPulseThreadState stateThreadState;

        private Clock timeProvider;

        private QuickPulseTimings timings;

        private bool isInitialized = false;

        private bool isPerformanceCollectorInitialized = false;

        private QuickPulseCollectionTimeSlotManager collectionTimeSlotManager = null;

        private IQuickPulseDataAccumulatorManager dataAccumulatorManager = null;

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
        /// <param name="serviceClient">QPS service client.</param>
        /// <param name="performanceCollector">Performance counter collector.</param>
        /// <param name="timings">Timings for the module.</param>
        internal QuickPulseTelemetryModule(
            QuickPulseCollectionTimeSlotManager collectionTimeSlotManager,
            QuickPulseDataAccumulatorManager dataAccumulatorManager,
            IQuickPulseServiceClient serviceClient,
            IPerformanceCollector performanceCollector,
            QuickPulseTimings timings)
            : this()
        {
            this.collectionTimeSlotManager = collectionTimeSlotManager;
            this.dataAccumulatorManager = dataAccumulatorManager;
            this.serviceClient = serviceClient;
            this.performanceCollector = performanceCollector;
            this.timings = timings;
        }

        /// <summary>
        /// Gets the QuickPulse service endpoint to send to.
        /// </summary>
        /// <remarks>Loaded from configuration.</remarks>
        public string QuickPulseServiceEndpoint { get; set; }

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

                        QuickPulseEventSource.Log.TroubleshootingMessageEvent("Validating configuration...");
                        this.ValidateConfiguration(configuration);
                        this.config = configuration;

                        QuickPulseEventSource.Log.TroubleshootingMessageEvent("Initializing members...");
                        this.collectionTimeSlotManager = this.collectionTimeSlotManager ?? new QuickPulseCollectionTimeSlotManager();
                        this.dataAccumulatorManager = this.dataAccumulatorManager ?? new QuickPulseDataAccumulatorManager();
                        this.performanceCollector = this.performanceCollector ?? new PerformanceCollector();
                        this.timeProvider = this.timeProvider ?? new Clock();
                        this.timings = timings ?? QuickPulseTimings.Default;

                        this.InitializeServiceClient(configuration);

                        this.stateManager = new QuickPulseCollectionStateManager(
                            this.serviceClient,
                            this.timeProvider,
                            this.timings,
                            this.OnStartCollection,
                            this.OnStopCollection,
                            this.OnSubmitSamples,
                            this.OnReturnFailedSamples);

                        this.CreateStateThread();

                        this.isInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Registers an instance of type <see cref="QuickPulseTelemetryProcessor"/> with this module.
        /// </summary>
        /// <remarks>This call is only necessary when the module is created in code and not in configuration.</remarks>
        /// <param name="telemetryProcessor">QuickPulseTelemetryProcessor instance to be registered with the module.</param>
        public void RegisterTelemetryProcessor(ITelemetryProcessor telemetryProcessor)
        {
            var quickPulseTelemetryProcessor = telemetryProcessor as IQuickPulseTelemetryProcessor;
            if (quickPulseTelemetryProcessor == null)
            {
                throw new ArgumentNullException(nameof(telemetryProcessor), @"The argument must be of type QuickPulseTelemetryProcessor");
            }

            lock (this.telemetryProcessorsLock)
            {
                const int MaxTelemetryProcessorCount = 100;
                if (!this.telemetryProcessors.Contains(quickPulseTelemetryProcessor))
                {
                    this.telemetryProcessors.AddLast(quickPulseTelemetryProcessor);

                    if (this.telemetryProcessors.Count > MaxTelemetryProcessorCount)
                    {
                        this.telemetryProcessors.RemoveFirst();
                    }

                    QuickPulseEventSource.Log.ProcessorRegistered(this.telemetryProcessors.Count.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private void EnsurePerformanceCollectorInitialized()
        {
            if (this.isPerformanceCollectorInitialized)
            {
                return;
            }

            this.isPerformanceCollectorInitialized = true;

            QuickPulseEventSource.Log.TroubleshootingMessageEvent("Initializing performance collector...");

            this.InitializePerformanceCollector();
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
                    // Instance placeholders are not currently supported since they require refresh. Refresh is not implemented at this time.
                    continue;
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
                catch (Exception e)
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

            if (configuration.TelemetryProcessors == null)
            {
                throw new ArgumentNullException(nameof(configuration.TelemetryProcessors));
            }
        }

        private void CreateStateThread()
        {
            this.stateThreadState = new QuickPulseThreadState();
            this.stateThread = new Thread(this.StateThreadWorker) { IsBackground = true };
            this.stateThread.Start();
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
                serviceEndpointUri = QuickPulseDefaults.ServiceEndpoint;
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
            string instanceName = GetInstanceName(configuration);
            string streamId = GetStreamId();
            var assemblyVersion = SdkVersionUtils.GetAssemblyVersion();
            this.serviceClient = new QuickPulseServiceClient(serviceEndpointUri, instanceName, streamId, assemblyVersion, this.timeProvider);

            QuickPulseEventSource.Log.TroubleshootingMessageEvent(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Service client initialized. Endpoint: '{0}', instance name: '{1}', assembly version: '{2}'",
                    serviceEndpointUri,
                    instanceName,
                    assemblyVersion));
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

        private static string GetStreamId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private void StateThreadWorker(object state)
        {
            var stopwatch = new Stopwatch();
            TimeSpan? timeToNextUpdate = null;

            while (true)
            {
                var currentCallbackStarted = this.timeProvider.UtcNow;

                try
                {
                    if (this.stateThreadState.IsStopRequested)
                    {
                        return;
                    }

                    stopwatch.Restart();

                    timeToNextUpdate = this.stateManager.UpdateState(this.config.InstrumentationKey);

                    QuickPulseEventSource.Log.StateTimerTickFinishedEvent(stopwatch.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    QuickPulseEventSource.Log.UnknownErrorEvent(e.ToInvariantString());
                }

                // the catastrophic fallback is for the case when we've catastrophically failed some place above
                timeToNextUpdate = timeToNextUpdate ?? this.timings.CatastrophicFailureTimeout;

                // try to factor in the time spend in this tick when scheduling the next one so that the average period is close to the intended
                TimeSpan timeSpentInThisTick = this.timeProvider.UtcNow - currentCallbackStarted;
                TimeSpan timeLeftUntilNextTick = timeToNextUpdate.Value - timeSpentInThisTick;
                timeLeftUntilNextTick = timeLeftUntilNextTick > TimeSpan.Zero ? timeLeftUntilNextTick : TimeSpan.Zero;

                Thread.Sleep(timeLeftUntilNextTick);
            }
        }

        private void CollectionThreadWorker(object state)
        {
            var stopwatch = new Stopwatch();
            var threadState = (QuickPulseThreadState)state;

            while (true)
            {
                try
                {
                    if (threadState.IsStopRequested)
                    {
                        return;
                    }

                    stopwatch.Restart();

                    this.CollectData();

                    QuickPulseEventSource.Log.CollectionTimerTickFinishedEvent(stopwatch.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    QuickPulseEventSource.Log.UnknownErrorEvent(e.ToInvariantString());
                }

                DateTimeOffset nextTick = this.collectionTimeSlotManager.GetNextCollectionTimeSlot(this.timeProvider.UtcNow);
                TimeSpan timeLeftUntilNextTick = nextTick - this.timeProvider.UtcNow;
                Thread.Sleep(timeLeftUntilNextTick > TimeSpan.Zero ? timeLeftUntilNextTick : TimeSpan.Zero);
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
                QuickPulseEventSource.Log.SampleStoredEvent(this.collectedSamples.Count + 1);

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
            QuickPulseEventSource.Log.TroubleshootingMessageEvent("Starting collection...");

            this.EndCollectionThread();

            this.EnsurePerformanceCollectorInitialized();

            this.dataAccumulatorManager.CompleteCurrentDataAccumulator();

            lock (this.telemetryProcessorsLock)
            {
                foreach (var telemetryProcessor in this.telemetryProcessors)
                {
                    telemetryProcessor.StartCollection(this.dataAccumulatorManager, this.serviceClient.ServiceUri, this.config);
                }
            }

            this.CreateCollectionThread();
        }

        private void CreateCollectionThread()
        {
            this.collectionThread = new Thread(this.CollectionThreadWorker) { IsBackground = true };
            this.collectionThreadState = new QuickPulseThreadState();
            this.collectionThread.Start(this.collectionThreadState);
        }

        private void EndCollectionThread()
        {
            if (this.collectionThreadState != null)
            {
                this.collectionThreadState.IsStopRequested = true;
            }
        }

        private void OnStopCollection()
        {
            QuickPulseEventSource.Log.TroubleshootingMessageEvent("Stopping collection...");

            this.EndCollectionThread();

            lock (this.telemetryProcessorsLock)
            {
                foreach (var telemetryProcessor in this.telemetryProcessors)
                {
                    telemetryProcessor.StopCollection();
                }
            }

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
            QuickPulseEventSource.Log.TroubleshootingMessageEvent("Returning samples...");

            lock (this.collectedSamplesLock)
            {
                foreach (var sample in samples)
                {
                    this.collectedSamples.AddFirst(sample);
                }

                while (this.collectedSamples.Count > MaxSampleStorageSize)
                {
                    this.collectedSamples.RemoveFirst();
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
                if (this.stateThread != null)
                {
                    if (this.stateThreadState != null)
                    {
                        this.stateThreadState.IsStopRequested = true;
                        this.stateThread.Join();
                    }

                    this.stateThread = null;
                }

                if (this.collectionThread != null)
                {
                    this.EndCollectionThread();
                    this.collectionThread.Join();
                    this.collectionThread = null;
                }
            }
        }
    }
}