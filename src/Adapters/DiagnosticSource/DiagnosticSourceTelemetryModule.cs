//-----------------------------------------------------------------------
// <copyright file="DiagnosticSourceTelemetryModule.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.DiagnosticSourceListener
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Implementation;

    /// <summary>
    /// A module to forward diagnostic source events to Application Insights.
    /// </summary>
    public sealed class DiagnosticSourceTelemetryModule : IObserver<DiagnosticListener>, ITelemetryModule, IDisposable
    {
        private TelemetryClient client;
        private IDisposable allDiagnosticListenersSubscription;
        private List<IDisposable> diagnosticListenerSubscriptions;

        /// <summary>
        /// Gets the list of DiagnosticSource listening requests (information about which DiagnosticSources should be traced).
        /// </summary>
        public IList<DiagnosticSourceListeningRequest> Sources { get; } = new List<DiagnosticSourceListeningRequest>();

        /// <summary>
        /// Initializes the telemetry module and starts tracing DiagnosticSources specified via the <see cref="Sources"/> property.
        /// </summary>
        /// <param name="configuration">The configuration object for the Application Insights pipeline.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("dsl:");
            this.allDiagnosticListenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
        }

        /// <summary>
        /// Dispose of this instance.
        /// </summary>
        public void Dispose()
        {
            if (this.diagnosticListenerSubscriptions != null)
            {
                foreach (var subscription in this.diagnosticListenerSubscriptions)
                {
                    subscription.Dispose();
                }
            }

            this.allDiagnosticListenersSubscription?.Dispose();
        }

        void IObserver<DiagnosticListener>.OnCompleted()
        {
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener listener)
        {
            foreach (var source in this.Sources)
            {
                if (source.Name == listener.Name)
                {
                    if (this.diagnosticListenerSubscriptions == null)
                    {
                        this.diagnosticListenerSubscriptions = new List<IDisposable>();
                    }

                    var subscription = new DiagnosticSourceListenerSubscription(listener.Name, this.client);
                    this.diagnosticListenerSubscriptions.Add(listener.Subscribe(subscription));
                    break;
                }
            }
        }
    }
}
