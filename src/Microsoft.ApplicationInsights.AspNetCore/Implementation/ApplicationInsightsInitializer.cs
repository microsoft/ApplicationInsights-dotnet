namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Logging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Class used to initialize Application Insight diagnostic listeners.
    /// </summary>
    internal class ApplicationInsightsInitializer : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> subscriptions;
        private readonly IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsInitializer"/> class.
        /// </summary>
        public ApplicationInsightsInitializer(
            IOptions<ApplicationInsightsServiceOptions> options,
            IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners,
            ILoggerFactory loggerFactory,
            DebugLoggerControl debugLoggerControl,
            TelemetryClient client)
        {
            this.diagnosticListeners = diagnosticListeners;
            this.subscriptions = new List<IDisposable>();

            // Add default logger factory for debug mode only if enabled and instrumentation key not set
            if (options.Value.EnableDebugLogger && string.IsNullOrEmpty(options.Value.InstrumentationKey))
            {
                // Do not use extension method here or it will disable debug logger we currently adding
                loggerFactory.AddProvider(new ApplicationInsightsLoggerProvider(client, (s, level) => debugLoggerControl.EnableDebugLogger && Debugger.IsAttached));
            }
        }

        /// <summary>
        /// Subscribes diagnostic listeners to sources
        /// </summary>
        public void Start()
        {
            DiagnosticListener.AllListeners.Subscribe(this);
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