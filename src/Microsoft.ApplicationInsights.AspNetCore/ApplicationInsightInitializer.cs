using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;

namespace Microsoft.ApplicationInsights.AspNetCore
{
    internal class ApplicationInsightInitializer: IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions;
        private readonly IEnumerable<IApplicationInsightDiagnosticListener> _diagnosticListeners;

        public ApplicationInsightInitializer(
            IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners)
        {
            _diagnosticListeners = diagnosticListeners;

            _subscriptions = new List<IDisposable>();
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