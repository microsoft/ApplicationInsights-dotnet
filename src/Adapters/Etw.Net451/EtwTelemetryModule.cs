namespace Microsoft.ApplicationInsights.EventSourceListener.EtwCollector
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.EventSource.Shared.Implementations;
    using Microsoft.ApplicationInsights.EventSourceListener.EtwCollector.Implemenetations;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Implementation;
    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Diagnostics.Tracing.Session;

    public class EtwTelemetryModule : ITelemetryModule, IDisposable
    {
        private TelemetryClient client;
        private bool isDisposed = false;
        private bool isInitialized = false;
        private ITraceEventSession traceEventSession;
        private List<Guid> enabledProviderIds;
        private List<string> enabledProviderNames;
        private readonly object lockObject;

        public IList<EtwListeningRequest> Sources { get; private set; }

        public EtwTelemetryModule() : this(
            new AITraceEventSession(
                new TraceEventSession($"ApplicationInsights-{nameof(EtwTelemetryModule)}-{Guid.NewGuid().ToString()}", TraceEventSessionOptions.Create)),
            new Action<ITraceEventSession, TelemetryClient>((traceSession, client) =>
            {
                if (traceSession != null && traceSession.Source != null)
                {
                    traceSession.Source.Dynamic.All += traceEvent =>
                    {
                        traceEvent.Track(client);
                    };
                    traceSession.Source.Process();
                }
            }))
        {
        }

        internal EtwTelemetryModule(ITraceEventSession traceEventSession,
            Action<ITraceEventSession, TelemetryClient> startTraceEventSessionAction)
        {
            this.lockObject = new object();
            this.Sources = new List<EtwListeningRequest>();
            this.enabledProviderIds = new List<Guid>();
            this.enabledProviderNames = new List<string>();

            this.traceEventSession = traceEventSession;
            this.StartTraceEventSession = startTraceEventSessionAction;
        }

        private Action<ITraceEventSession, TelemetryClient> StartTraceEventSession
        {
            get;
            set;
        }

        public void Initialize(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                EventSourceListenerEventSource.Log.ModuleInitializationFailed(nameof(EtwTelemetryModule),
                    $"Argument {nameof(configuration)} is required. The initialization is terminated.");
                return;
            }

            if (this.isDisposed)
            {
                EventSourceListenerEventSource.Log.ModuleInitializationFailed(nameof(EtwTelemetryModule),
                    "Can't initialize a module that is disposed. The initialization is terminated.");
                return;
            }

            bool? isProcessElevated = this.traceEventSession.IsElevated();
            if (!isProcessElevated.HasValue || !isProcessElevated.Value)
            {
                EventSourceListenerEventSource.Log.ModuleInitializationFailed(nameof(EtwTelemetryModule),
                    "The process is required to be elevated to enable ETW providers. The initialization is terminated.");
                return;
            }

            lock (this.lockObject)
            {
                this.client = new TelemetryClient(configuration);

                // sdkVersionIdentifier will be used in telemtry entry as a identifier for the sender.
                // The value will look like: etw:x.x.x-x
                const string sdkVersionIdentifier = "etw:";
                this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion(sdkVersionIdentifier);


                if (this.isInitialized)
                {
                    try
                    {
                        this.traceEventSession?.Stop();
                    }
                    catch (Exception ex)
                    {
                        // Log failed stopping the trace session.
                        EventSourceListenerEventSource.Log.ModuleInitializationFailed(nameof(EtwTelemetryModule), ex.Message);
                    }
                    finally
                    {
                        this.isInitialized = false;
                    }

                    DisableProviders();
                    this.enabledProviderIds.Clear();
                    this.enabledProviderNames.Clear();
                }

                if (this.Sources.Count == 0)
                {
                    EventSourceListenerEventSource.Log.NoSourcesConfigured(moduleName: nameof(EtwTelemetryModule));
                    return;
                }

                EnableProviders();
                try
                {
                    // Start the trace session
                    Task.Factory.StartNew(() => this.StartTraceEventSession(this.traceEventSession, this.client), TaskCreationOptions.LongRunning);
                }
                finally
                {
                    this.isInitialized = true;
                }
            }
        }

        private void EnableProviders()
        {
            foreach (EtwListeningRequest request in this.Sources)
            {
                EnableProvider(request);
            }
        }

        private void EnableProvider(EtwListeningRequest request)
        {
            try
            {
                request.Validate();
            }
            catch (Exception ex)
            {
                EventSourceListenerEventSource.Log.FailedToEnableProviders(nameof(EtwTelemetryModule),
                    string.IsNullOrEmpty(request.ProviderName) ? request.ProviderGuid.ToString() : request.ProviderName,
                    ex.Message);
            }

            try
            {
                if (request.ProviderGuid != Guid.Empty)
                {
                    EnableProvider(request.ProviderGuid, request.Level, request.Keywords);
                }
                else
                {
                    EnableProvider(request.ProviderName, request.Level, request.Keywords);
                }
            }
            catch (Exception ex)
            {
                EventSourceListenerEventSource.Log.FailedToEnableProviders(nameof(EtwTelemetryModule),
                    string.IsNullOrEmpty(request.ProviderName) ? request.ProviderGuid.ToString() : request.ProviderName,
                    ex.Message);
            }
        }

        private void EnableProvider(Guid providerGuid, TraceEventLevel level, ulong keywords)
        {
            this.traceEventSession.EnableProvider(providerGuid, level, keywords);
            enabledProviderIds.Add(providerGuid);
        }

        private void EnableProvider(string providerName, TraceEventLevel level, ulong keywords)
        {
            this.traceEventSession.EnableProvider(providerName, level, keywords);
            enabledProviderNames.Add(providerName);
        }

        private void DisableProviders()
        {
            foreach (Guid id in enabledProviderIds)
            {
                this.traceEventSession.DisableProvider(id);
            }
            foreach (string providerName in enabledProviderNames)
            {
                this.traceEventSession.DisableProvider(providerName);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool isDisposing)
        {
            if (this.isDisposed) return;

            // Mark this object as disposed even when disposing run into exception, which is not expected.
            this.isDisposed = true;
            if (isDisposing)
            {
                if (traceEventSession != null)
                {
                    traceEventSession.Dispose();
                }
            }
        }
    }
}
