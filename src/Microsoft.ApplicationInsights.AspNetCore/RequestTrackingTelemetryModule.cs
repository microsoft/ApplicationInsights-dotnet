namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Telemetry module tracking requests using Diagnostic Listeners.
    /// </summary>
    public class RequestTrackingTelemetryModule : ITelemetryModule, IObserver<DiagnosticListener>, IDisposable
    {
        private TelemetryClient telemetryClient;
        private IApplicationIdProvider applicationIdProvider;
        private readonly List<IDisposable> subscriptions;
        private List<IApplicationInsightDiagnosticListener> diagnosticListeners;
        private bool isInitialized = false;
        private readonly object lockObject = new object();
        private TelemetryConfiguration configuration;

        public RequestTrackingTelemetryModule(IApplicationIdProvider applicationIdProvider)
        {
            this.applicationIdProvider = applicationIdProvider;
            this.subscriptions = new List<IDisposable>();
            this.diagnosticListeners = new List<IApplicationInsightDiagnosticListener>();
        }

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
                            (new HostingDiagnosticListener(this.telemetryClient, applicationIdProvider));

                        this.diagnosticListeners.Add
                            (new MvcDiagnosticsListener());

                        this.subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));

                        this.isInitialized = true;
                    }
                }
            }
        }

        /// <inheritdoc />
        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            foreach (var applicationInsightDiagnosticListener in this.diagnosticListeners)
            {
                if (applicationInsightDiagnosticListener.ListenerName == value.Name)
                {
                    this.subscriptions.Add(value.SubscribeWithAdapter(applicationInsightDiagnosticListener));
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
            if (disposing)
            {
                foreach (var subscription in this.subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }
    }
}