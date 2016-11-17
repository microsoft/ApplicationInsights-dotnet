using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.AspNetCore.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.ApplicationInsights.AspNetCore
{
    internal class ApplicationInsightsInitializer: IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions;
        private readonly IEnumerable<IApplicationInsightDiagnosticListener> _diagnosticListeners;

        public ApplicationInsightsInitializer(IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners,
            IOptions<ApplicationInsightsServiceOptions> options,
            TelemetryClient telemetryClient,
            ILoggerFactory loggerFactory)
        {
            _diagnosticListeners = diagnosticListeners;
            _subscriptions = new List<IDisposable>();

            loggerFactory.AddProvider(new ApplicationInsightsLoggerProvider(telemetryClient, options.Value.LoggerMinimumLevel));
        }

        public void Start()
        {
            DiagnosticListener.AllListeners.Subscribe(this);
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            foreach (var applicationInsightDiagnosticListener in _diagnosticListeners)
            {
                if (applicationInsightDiagnosticListener.ListenerName == value.Name)
                {
                    _subscriptions.Add(value.SubscribeWithAdapter(applicationInsightDiagnosticListener));
                }
            }
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
        }

        void IObserver<DiagnosticListener>.OnCompleted()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}