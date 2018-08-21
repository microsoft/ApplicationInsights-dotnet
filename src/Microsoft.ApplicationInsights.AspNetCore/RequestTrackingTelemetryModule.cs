namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Telemetry module tracking requests using Diagnostic Listeners.
    /// </summary>
    public class RequestTrackingTelemetryModule : ITelemetryModule, IObserver<DiagnosticListener>, IDisposable
    {
        private TelemetryClient telemetryClient;
        private readonly IApplicationIdProvider applicationIdProvider;
        private ConcurrentBag<IDisposable> subscriptions;
        private readonly List<IApplicationInsightDiagnosticListener> diagnosticListeners;
        private bool isInitialized = false;
        private readonly object lockObject = new object();

        /// <summary>
        /// RequestTrackingTelemetryModule
        /// </summary>
        public RequestTrackingTelemetryModule() : this(null)
        {
            this.CollectionOptions = new RequestCollectionOptions();
        }

        /// <summary>
        /// Creates RequestTrackingTelemetryModule.
        /// </summary>
        /// <param name="applicationIdProvider"></param>
        public RequestTrackingTelemetryModule(IApplicationIdProvider applicationIdProvider)
        {
            this.applicationIdProvider = applicationIdProvider;
            this.subscriptions = new ConcurrentBag<IDisposable>();
            this.diagnosticListeners = new List<IApplicationInsightDiagnosticListener>();
        }

        /// <summary>
        /// Gets or sets request collection options.
        /// </summary>
        public RequestCollectionOptions CollectionOptions { get; set; }

        /// <summary>
        /// Initializes the telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry configuration to use for initialization.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        this.telemetryClient = new TelemetryClient(configuration);

                        this.diagnosticListeners.Add
                            (new HostingDiagnosticListener(
                            this.telemetryClient,
                            this.applicationIdProvider,
                            this.CollectionOptions.InjectResponseHeaders,
                            this.CollectionOptions.TrackExceptions,
                            this.CollectionOptions.EnableW3CDistributedTracing));

                        this.diagnosticListeners.Add
                            (new MvcDiagnosticsListener());

                        this.subscriptions?.Add(DiagnosticListener.AllListeners.Subscribe(this));

                        this.isInitialized = true;
                    }
                }
            }
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            var subs = Volatile.Read(ref this.subscriptions);
            if (subs is null)
            {
                return;
            }

            foreach (var applicationInsightDiagnosticListener in this.diagnosticListeners)
            {
                if (applicationInsightDiagnosticListener.ListenerName == value.Name)
                {
                    subs.Add(value.SubscribeWithAdapter(applicationInsightDiagnosticListener));
                }
            }
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnCompleted()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            var subs = Interlocked.Exchange(ref this.subscriptions, null);
            if (subs is null)
            {
                return;
            }

            foreach (var subscription in subs)
            {
                subscription.Dispose();
            }
        }
    }
}