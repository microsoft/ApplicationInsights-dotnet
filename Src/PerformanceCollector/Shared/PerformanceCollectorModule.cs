namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;

    using Timer = Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.Timer.Timer;

    /// <summary>
    /// Telemetry module for collecting performance counters.
    /// </summary>
    public sealed class PerformanceCollectorModule : ITelemetryModule, IDisposable
    {
        private readonly object lockObject = new object();

        private readonly List<string> defaultCounters = new List<string>()
                                                            {
                                                                @"\Process(??APP_WIN32_PROC??)\% Processor Time",
                                                                @"\Memory\Available Bytes",
                                                                @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests/Sec",
                                                                @"\.NET CLR Exceptions(??APP_CLR_PROC??)\# of Exceps Thrown / sec",
                                                                @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Request Execution Time",
                                                                @"\Process(??APP_WIN32_PROC??)\Private Bytes",
                                                                @"\Process(??APP_WIN32_PROC??)\IO Data Bytes/sec",
                                                                @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests In Application Queue",
                                                                @"\Processor(_Total)\% Processor Time"
                                                            };
            
        private readonly IPerformanceCollector collector = new PerformanceCollector();

        /// <summary>
        /// Determines how often collection takes place.
        /// </summary>
        private readonly TimeSpan collectionPeriod = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Determines how often we re-register performance counters.
        /// </summary>
        /// <remarks>Re-registration is needed because IIS starts reporting on different counter instances depending
        /// on worker processes starting and termination.</remarks>
        private readonly TimeSpan registrationPeriod = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The timestamp of last performance counter registration.
        /// </summary>
        private DateTime lastRefreshTimestamp = DateTime.MinValue;

        /// <summary>
        /// Communication sink for performance data.
        /// </summary>
        /// <remarks>
        /// TelemetryContext is thread-safe.
        /// </remarks>
        private TelemetryClient client = null;

        // Used in unit tests only
        private TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Timer to schedule performance collection.
        /// </summary>
        private Timer timer = null;

        private bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCollectorModule"/> class.
        /// </summary>
        public PerformanceCollectorModule()
        {
            this.Counters = new List<PerformanceCounterCollectionRequest>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCollectorModule"/> class.
        /// </summary>
        /// <remarks>Unit tests only.</remarks>
        internal PerformanceCollectorModule(IPerformanceCollector collectorMock) : this()
        {
            this.collector = collectorMock;
        }

        /// <summary>
        /// Gets a list of counters to collect.
        /// </summary>
        /// <remarks>Loaded from configuration.</remarks>
        public IList<PerformanceCounterCollectionRequest> Counters { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether performance counters should be collected under IIS Express.
        /// </summary>
        /// <remarks>Loaded from configuration.</remarks>
        public bool EnableIISExpressPerformanceCounters { get; set; }

        /// <summary>
        /// Initialize method is called after all configuration properties have been loaded from the configuration.
        /// </summary>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        PerformanceCollectorEventSource.Log.ModuleIsBeingInitializedEvent(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Custom counters count: '{0}'",
                                this.Counters != null ? this.Counters.Count : 0));

                        if (configuration == null)
                        {
                            throw new ArgumentNullException("configuration");
                        }

                        if (!this.EnableIISExpressPerformanceCounters && IsRunningUnderIisExpress())
                        {
                            PerformanceCollectorEventSource.Log.RunningUnderIisExpress();
                            return;
                        }

                        this.telemetryConfiguration = configuration;
                        this.client = new TelemetryClient(configuration);

                        this.lastRefreshTimestamp = DateTime.MinValue;

                        this.timer = new Timer(this.TimerCallback);

                        // schedule the first tick
                        this.timer.ScheduleNextTick(this.collectionPeriod);
                        this.isInitialized = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// Disposes resources allocated by this type.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static ITelemetry CreateTelemetry(PerformanceCounter pc, string reportAs, bool isCustomCounter, float value)
        {
            if (isCustomCounter)
            {
                // string.Format(CultureInfo.InvariantCulture, @"\{0}\{1}", pc.CategoryName, pc.CounterName),
                var metricName = !string.IsNullOrWhiteSpace(reportAs)
                                     ? reportAs
                                     : string.Format(
                                         CultureInfo.InvariantCulture,
                                         "{0} - {1}",
                                         pc.CategoryName,
                                         pc.CounterName);
                
                var metricTelemetry = new MetricTelemetry(metricName, value);

                metricTelemetry.Properties.Add("CounterInstanceName", pc.InstanceName);
                metricTelemetry.Properties.Add("CustomPerfCounter", "true");

                return metricTelemetry;
            }
            else
            {
                return new PerformanceCounterTelemetry(pc.CategoryName, pc.CounterName, pc.InstanceName, value);
            }
        }

        private static bool IsRunningUnderIisExpress()
        {
            var iisExpressProcessName = "iisexpress";

            try
            {
                return Process.GetCurrentProcess().ProcessName.IndexOf(iisExpressProcessName, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception e)
            {
                // we are unable to determine if we're running under IIS Express, assume we are not
                PerformanceCollectorEventSource.Log.UnknownErrorEvent(
                    string.Format(CultureInfo.InvariantCulture, "Unable to get process name. {0}", e.ToInvariantString()));

                return false;
            }
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

        private void TimerCallback(object state)
        {
            try
            {
                PerformanceCollectorEventSource.Log.CounterCollectionAttemptEvent();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                this.EnsurePerformanceCountersRegistered();

                var results =
                    this.collector.Collect(
                        (counterName, e) =>
                        PerformanceCollectorEventSource.Log.CounterReadingFailedEvent(e.ToString(), counterName))
                        .ToList();

                stopwatch.Stop();

                PerformanceCollectorEventSource.Log.CounterCollectionSuccessEvent(
                    results.LongCount(),
                    stopwatch.ElapsedMilliseconds);

                foreach (var result in results)
                {
                    var telemetry = CreateTelemetry(
                        result.Item1.PerformanceCounter,
                        result.Item1.ReportAs,
                        result.Item1.IsCustomCounter,
                        result.Item2);

                    try
                    {
                        this.client.Track(telemetry);
                    }
                    catch (InvalidOperationException e)
                    {
                        PerformanceCollectorEventSource.Log.TelemetrySendFailedEvent(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                PerformanceCollectorEventSource.Log.UnknownErrorEvent(e.ToString());
            }
            finally
            {
                if (this.timer != null)
                {
                    this.timer.ScheduleNextTick(this.collectionPeriod);
                }
            }
        }

        /// <summary>
        /// Binds processes to performance counters instance names and adds performance counters to the collection.
        /// </summary>
        /// <remarks>This operation is expensive, but must be done periodically to account for IIS changing instance names
        /// of the counters it reports Web Sites on as worker processes start and terminate.</remarks>
        private void EnsurePerformanceCountersRegistered()
        {
            if (DateTime.Now - this.lastRefreshTimestamp < this.registrationPeriod)
            {
                // re-registration period hasn't elapsed yet, do nothing
                return;
            }
            
            // get all instances that currently exist in the system
            var win32Instances = PerformanceCounterUtility.GetWin32ProcessInstances();
            var clrInstances = PerformanceCounterUtility.GetClrProcessInstances();

            PerformanceCounterUtility.InvalidatePlaceholderCache();

            if (this.lastRefreshTimestamp == DateTime.MinValue)
            {
                // this is the initial registration, register everything
                this.ProcessCustomCounters();

                string error;
                var errors = new List<string>();
                
                this.defaultCounters.ForEach(pcName => this.RegisterCounter(pcName, string.Empty, win32Instances, clrInstances, false, out error));

                foreach (PerformanceCounterCollectionRequest req in this.Counters)
                {
                    this.RegisterCounter(
                        req.PerformanceCounter,
                        req.ReportAs,
                        win32Instances,
                        clrInstances,
                        true,
                        out error);

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        errors.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.PerformanceCounterCheckConfigurationEntry,
                                req.PerformanceCounter,
                                error));
                    }
                }

                if (errors.Any())
                {
                    // send out the unified error message
                    PerformanceCollectorEventSource.Log.CounterCheckConfigurationEvent(
                        errors.Count.ToString(CultureInfo.InvariantCulture),
                        this.Counters.Count.ToString(CultureInfo.InvariantCulture),
                        string.Join(Environment.NewLine, errors));
                }
            }
            else
            {
                // this is a periodic refresh
                this.RefreshCounters(win32Instances, clrInstances);
            }

            // as per MSDN, we need to wait at least 1s before proceeding with counter collection
            Thread.Sleep(TimeSpan.FromSeconds(1));

            this.lastRefreshTimestamp = DateTime.Now;
        }

        private void RefreshCounters(IEnumerable<string> win32Instances, IEnumerable<string> clrInstances)
        {
            // we need to refresh counters in bad state and counters with placeholders in instance names
            var countersToRefresh =
                this.collector.PerformanceCounters.Where(pc => pc.IsInBadState || pc.UsesInstanceNamePlaceholder)
                    .ToList();

            countersToRefresh.ForEach(pcd => this.RefreshCounter(pcd, win32Instances, clrInstances));

            PerformanceCollectorEventSource.Log.CountersRefreshedEvent(countersToRefresh.Count.ToString(CultureInfo.InvariantCulture));
        }

        private void RefreshCounter(
            PerformanceCounterData pcd,
            IEnumerable<string> win32Instances,
            IEnumerable<string> clrInstances)
        {
            string dummy;

            bool usesInstanceNamePlaceholder;
            var pc = this.CreateCounter(
                pcd.OriginalString,
                win32Instances,
                clrInstances,
                out usesInstanceNamePlaceholder,
                out dummy);

            try
            {
                this.collector.RefreshPerformanceCounter(pcd, pc);

                PerformanceCollectorEventSource.Log.CounterRegisteredEvent(
                        PerformanceCounterUtility.FormatPerformanceCounter(pc));
            }
            catch (InvalidOperationException e)
            {
                PerformanceCollectorEventSource.Log.CounterRegistrationFailedEvent(
                    e.Message,
                    PerformanceCounterUtility.FormatPerformanceCounter(pc));
            }
        }

        private PerformanceCounter CreateCounter(
            string perfCounterName,
            IEnumerable<string> win32Instances,
            IEnumerable<string> clrInstances,
            out bool usesInstanceNamePlaceholder,
            out string error)
        {
            error = null;

            try
            {
                return PerformanceCounterUtility.ParsePerformanceCounter(
                    perfCounterName,
                    win32Instances,
                    clrInstances,
                    out usesInstanceNamePlaceholder);
            }
            catch (Exception e)
            {
                usesInstanceNamePlaceholder = false;
                PerformanceCollectorEventSource.Log.CounterParsingFailedEvent(e.Message, perfCounterName);
                error = e.Message;

                return null;
            }
        }

        private void RegisterCounter(
            string perfCounterName,
            string reportAs,
            IList<string> win32Instances,
            IList<string> clrInstances,
            bool isCustomCounter,
            out string error)
        {
            bool usesInstanceNamePlaceholder;
            var pc = this.CreateCounter(
                perfCounterName,
                win32Instances,
                clrInstances,
                out usesInstanceNamePlaceholder,
                out error);

            if (pc != null)
            {
                this.RegisterCounter(perfCounterName, reportAs, pc, isCustomCounter, usesInstanceNamePlaceholder, out error);
            }
        }

        private void RegisterCounter(
            string originalString,
            string reportAs,
            PerformanceCounter pc,
            bool isCustomCounter,
            bool usesInstanceNamePlaceholder,
            out string error)
        {
            error = null;

            try
            {
                this.collector.RegisterPerformanceCounter(
                    originalString,
                    reportAs,
                    pc.CategoryName,
                    pc.CounterName,
                    pc.InstanceName,
                    usesInstanceNamePlaceholder,
                    isCustomCounter);

                PerformanceCollectorEventSource.Log.CounterRegisteredEvent(
                    PerformanceCounterUtility.FormatPerformanceCounter(pc));
            }
            catch (InvalidOperationException e)
            {
                PerformanceCollectorEventSource.Log.CounterRegistrationFailedEvent(
                    e.Message,
                    PerformanceCounterUtility.FormatPerformanceCounter(pc));
                error = e.Message;
            }
        }

        private void ProcessCustomCounters()
        {
            // remove duplicate counters
            this.Counters = this.Counters.GroupBy(req => req.PerformanceCounter).Select(g => g.First()).ToList();

            // assign and sanitize reportsAs
            int i = 0;
            foreach (PerformanceCounterCollectionRequest req in this.Counters)
            {
                req.ReportAs = string.IsNullOrWhiteSpace(req.ReportAs)
                    ? req.PerformanceCounter
                    : req.ReportAs;

                req.ReportAs = this.SanitizeReportAs(req.ReportAs, req.PerformanceCounter, ref i);
            }
        }

        private string SanitizeReportAs(string reportAs, string performanceCounter, ref int counterIndex)
        {
            string newReportAs = reportAs.Trim();

            // check if anything is left
            if (string.IsNullOrWhiteSpace(newReportAs))
            {
                // nothing is left, generate a "random" name
                var c = Convert.ToChar('A' + counterIndex);

                if (!char.IsLetter(c))
                {
                    // we have run out of letters, this should only happen when all counters are unicode
                    // not much we can do, the customer won't be able to differentiate between counters anyway
                    counterIndex = 0;
                    c = Convert.ToChar('A' + counterIndex);
                }

                newReportAs = string.Format(CultureInfo.InvariantCulture, Resources.UnicodePerformanceCounterName, c);

                counterIndex++;
            }

            if (!string.Equals(reportAs, newReportAs, StringComparison.Ordinal))
            {
                PerformanceCollectorEventSource.Log.CounterReportAsStrippedEvent(
                    performanceCounter,
                    newReportAs,
                    reportAs);
            }

            return newReportAs;
        }
    }
}