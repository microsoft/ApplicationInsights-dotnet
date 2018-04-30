namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Threading;

    /// <summary>
    /// Class used to initialize Application Insight diagnostic listeners.
    /// </summary>
    internal class ApplicationInsightsInitializer : IObserver<DiagnosticListener>, IDisposable
    {
        private ConcurrentBag<IDisposable> subscriptions;
        private readonly IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsInitializer"/> class.
        /// </summary>
        public ApplicationInsightsInitializer(
            IOptions<ApplicationInsightsServiceOptions> options,
            IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            this.diagnosticListeners = diagnosticListeners;
            this.subscriptions = new ConcurrentBag<IDisposable>();

            // Add default logger factory for debug mode only if enabled and instrumentation key not set
            if (options.Value.EnableDebugLogger && string.IsNullOrEmpty(options.Value.InstrumentationKey))
            {
                // Do not use extension method here or it will disable debug logger we currently adding
                var enableDebugLogger = true;
                loggerFactory.AddApplicationInsights(serviceProvider, (s, level) => enableDebugLogger && Debugger.IsAttached, () => enableDebugLogger = false);
            }
        }

        /// <summary>
        /// Subscribes diagnostic listeners to sources
        /// </summary>
        public void Start()
        {
            this.subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
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
