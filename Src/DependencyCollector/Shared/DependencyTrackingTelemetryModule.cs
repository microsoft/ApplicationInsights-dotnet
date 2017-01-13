namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.Diagnostics.Instrumentation.Extensions.Intercept;

    /// <summary>
    /// Remote dependency monitoring.
    /// </summary>
    public class DependencyTrackingTelemetryModule : ITelemetryModule, IDisposable
    {
        private readonly object lockObject = new object();

#if !NET40
        // Net40 does not support framework event source
        private FrameworkHttpEventListener httpEventListener;
        private FrameworkSqlEventListener sqlEventListener;
#endif

        private ProfilerSqlProcessing sqlProcessing;
        private ProfilerHttpProcessing httpProcessing;        
        private TelemetryConfiguration telemetryConfiguration;
        private bool isInitialized = false;
        private bool disposed = false;
        private bool correlationHeadersEnabled = true;
        private ICollection<string> excludedCorrelationDomains = new SanitizedHostList();

        /// <summary>
        /// Gets or sets a value indicating whether to disable runtime instrumentation.
        /// </summary>
        public bool DisableRuntimeInstrumentation { get; set; }

        /// <summary>
        /// Gets the component correlation configuration.
        /// </summary>
        public ICollection<string> ExcludeComponentCorrelationHttpHeadersOnDomains
        {
            get
            {
                return this.excludedCorrelationDomains;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the correlation headers would be set on outgoing http requests.
        /// </summary>
        public bool SetComponentCorrelationHttpHeaders
        {
            get
            {
                return this.correlationHeadersEnabled;
            }

            set
            {
                this.correlationHeadersEnabled = value;
            }
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize method is called after all configuration properties have been loaded from the configuration.
        /// </summary>
        public void Initialize(TelemetryConfiguration configuration)
        {
            // Temporary fix to make sure that we initialize module once.
            // It should be removed when configuration reading logic is moved to Web SDK.
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        try
                        {                            
                            this.telemetryConfiguration = configuration;

                            // Net40 only supports runtime instrumentation
                            // Net45 supports either but not both to avoid duplication
                            this.InitializeForRuntimeInstrumentationOrFramework();
                        }
                        catch (Exception exc)
                        {
                            DependencyCollectorEventSource.Log.RemoteDependencyModuleError(exc.ToInvariantString(), Environment.Version.ToString());
                        }

                        this.isInitialized = true;
                    }
                }
            }
        }

        internal virtual void InitializeForRuntimeProfiler()
        {
            // initialize instrumentation extension
            var extensionBaseDirectory = string.IsNullOrWhiteSpace(AppDomain.CurrentDomain.RelativeSearchPath)
                ? AppDomain.CurrentDomain.BaseDirectory
                : AppDomain.CurrentDomain.RelativeSearchPath;

            DependencyCollectorEventSource.Log.RemoteDependencyModuleInformation("extesionBaseDirectrory is " + extensionBaseDirectory);
            Decorator.InitializeExtension(extensionBaseDirectory);

            // obtain agent version
            var agentVersion = Decorator.GetAgentVersion();
            DependencyCollectorEventSource.Log.RemoteDependencyModuleInformation("AgentVersion is " + agentVersion);

            this.httpProcessing = new ProfilerHttpProcessing(this.telemetryConfiguration, agentVersion, DependencyTableStore.Instance.WebRequestConditionalHolder, this.SetComponentCorrelationHttpHeaders, this.ExcludeComponentCorrelationHttpHeadersOnDomains);
            this.sqlProcessing = new ProfilerSqlProcessing(this.telemetryConfiguration, agentVersion, DependencyTableStore.Instance.SqlRequestConditionalHolder);

            ProfilerRuntimeInstrumentation.DecorateProfilerForHttp(ref this.httpProcessing);
            ProfilerRuntimeInstrumentation.DecorateProfilerForSql(ref this.sqlProcessing);
        }

        internal virtual bool IsProfilerAvailable()
        {
            return Decorator.IsHostEnabled();
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        /// <param name="disposing">The method has been called directly or indirectly by a user's code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
#if !NET40
                    // Net40 does not support framework event source
                    if (this.httpEventListener != null)
                    {
                        this.httpEventListener.Dispose();
                    }

                    if (this.sqlEventListener != null)
                    {
                        this.sqlEventListener.Dispose();
                    }
#endif
                }
                
                this.disposed = true;
            }
        }

        /// <summary>
        /// Initialize for framework event source (not supported for Net40).
        /// </summary>
        private void InitializeForFrameworkEventSource()
        {
#if !NET40
            // In 4.5 EventListener has a race condition issue in constructor so we retry to create listeners
            this.httpEventListener = RetryPolicy.Retry<InvalidOperationException, TelemetryConfiguration, FrameworkHttpEventListener>(
                config => new FrameworkHttpEventListener(config, DependencyTableStore.Instance.WebRequestCacheHolder),
                this.telemetryConfiguration,
                TimeSpan.FromMilliseconds(10));

            this.sqlEventListener = RetryPolicy.Retry<InvalidOperationException, TelemetryConfiguration, FrameworkSqlEventListener>(
                config => new FrameworkSqlEventListener(config, DependencyTableStore.Instance.SqlRequestCacheHolder),
                this.telemetryConfiguration,
                TimeSpan.FromMilliseconds(10));
#endif
        }

        /// <summary>
        /// Initialize for runtime instrumentation or framework event source.
        /// </summary>
        private void InitializeForRuntimeInstrumentationOrFramework()
        {
            if (this.IsProfilerAvailable())
            {
                DependencyCollectorEventSource.Log.RemoteDependencyModuleInformation("Profiler is attached.");
                if (!this.DisableRuntimeInstrumentation)
                {
                    try
                    {
                        this.InitializeForRuntimeProfiler();
                        DependencyTableStore.Instance.IsProfilerActivated = true;
                    }
                    catch (Exception exp)
                    {
                        this.InitializeForFrameworkEventSource();
                        DependencyCollectorEventSource.Log.ProfilerFailedToAttachError(exp.ToInvariantString());
                    }
                }
                else
                {
                    // if config is set to disable runtime instrumentation then default to framework event source
                    this.InitializeForFrameworkEventSource();
                    DependencyCollectorEventSource.Log.RemoteDependencyModuleVerbose("Runtime instrumentation is set to disabled. Initialize with framework event source instead.");
                }
            }
            else
            {
                // if profiler is not attached then default to framework event source
                this.InitializeForFrameworkEventSource();

                // Log a message to indicate the profiler is not attached
                DependencyCollectorEventSource.Log.RemoteDependencyModuleProfilerNotAttached();
            }
        }
    }
}
