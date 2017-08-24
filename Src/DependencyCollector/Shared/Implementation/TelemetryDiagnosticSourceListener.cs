namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.Implementation;

    internal class TelemetryDiagnosticSourceListener : IObserver<DiagnosticListener>, IDisposable
    {
        private const string ActivityNameSuffix = ".OutboundCall";
        private const string StopSuffix = ".Stop";
        private const string ActivityStopNameSuffix = ActivityNameSuffix + StopSuffix;

        private readonly TelemetryClient client;
        private readonly TelemetryConfiguration configuration;

        private readonly IDisposable listenerSubscription;
        private List<IDisposable> individualSubscriptions;

        public TelemetryDiagnosticSourceListener(TelemetryConfiguration configuration)
        {
            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceListener + ":");

            this.configuration = configuration;

            try
            {
                this.listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.TelemetryDiagnosticSourceListenerFailedToSubscribe(ex.ToInvariantString());
            }
        }

        /// <summary>
        /// This method gets called once for each existing DiagnosticListener when this
        /// DiagnosticListener is added to the list of DiagnosticListeners
        /// (<see cref="System.Diagnostics.DiagnosticListener.AllListeners"/>). This method will
        /// also be called for each subsequent DiagnosticListener that is added to the list of
        /// DiagnosticListeners.
        /// <seealso cref="IObserver{T}.OnNext(T)"/>
        /// </summary>
        /// <param name="value">The DiagnosticListener that exists when this listener was added to
        /// the list, or a DiagnosticListener that got added after this listener was added.</param>
        public void OnNext(DiagnosticListener value)
        {
            if (value != null)
            {
                IDisposable subscription = value.Subscribe(
                    new IndividualDiagnosticSourceListener(value, this.client),
                    (evnt, r, _) => evnt.EndsWith(ActivityNameSuffix, StringComparison.OrdinalIgnoreCase)
                        || evnt.EndsWith(ActivityStopNameSuffix, StringComparison.OrdinalIgnoreCase));

                if (this.individualSubscriptions == null)
                {
                    this.individualSubscriptions = new List<IDisposable>();
                }
                this.individualSubscriptions.Add(subscription);
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// <seealso cref="IObserver{T}.OnCompleted()"/>
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// <seealso cref="IObserver{T}.OnError(Exception)"/>
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
        }

        public void Dispose()
        {
            if (this.individualSubscriptions != null)
            {
                foreach (var individualSubscription in this.individualSubscriptions)
                {
                    individualSubscription.Dispose();
                }
            }

            if (this.listenerSubscription != null)
            {
                this.listenerSubscription.Dispose();
            }
        }


        /// <summary>
        /// Diagnostic listener implementation that listens for events specific to outgoing dependency requests.
        /// </summary>
        private class IndividualDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>
        {
            private readonly DiagnosticListener diagnosticListener;
            private readonly TelemetryClient client;

            internal IndividualDiagnosticSourceListener(DiagnosticListener diagnosticListener, TelemetryClient client)
            {
                this.diagnosticListener = diagnosticListener;
                this.client = client;
            }

        public void OnNext(KeyValuePair<string, object> evnt)
            {
                if (evnt.Key.EndsWith(ActivityStopNameSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    this.OnActivityStop();
                }
            }

            /// <summary>
            /// Notifies the observer that the provider has finished sending push-based notifications.
            /// <seealso cref="IObserver{T}.OnCompleted()"/>
            /// </summary>
            public void OnCompleted()
            {
            }

            /// <summary>
            /// Notifies the observer that the provider has experienced an error condition.
            /// <seealso cref="IObserver{T}.OnError(Exception)"/>
            /// </summary>
            /// <param name="error">An object that provides additional information about the error.</param>
            public void OnError(Exception error)
            {
            }

            /// <summary>
            /// Handler for Activity stop event (response is received for the outgoing request).
            /// </summary>
            internal void OnActivityStop()
            {
                Activity currentActivity = Activity.Current;
                if (currentActivity == null)
                {
                    DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                    return;
                }

                DependencyCollectorEventSource.Log.TelemetryDiagnosticSourceListenerActivityStopped(currentActivity.Id, currentActivity.OperationName);

                DependencyTelemetry telemetry = new DependencyTelemetry();

                // properly fill dependency telemetry operation context
                telemetry.Context.Operation.Id = currentActivity.RootId;
                telemetry.Context.Operation.ParentId = currentActivity.ParentId;
                telemetry.Id = currentActivity.Id;
                telemetry.Duration = currentActivity.Duration;
                telemetry.Timestamp = currentActivity.StartTimeUtc;

                foreach (var item in currentActivity.Baggage)
                {
                    if (!telemetry.Context.Properties.ContainsKey(item.Key))
                    {
                        telemetry.Context.Properties[item.Key] = item.Value;
                    }
                }

                this.client.Initialize(telemetry);

                // get the Uri from event payload
                Uri requestUri = null;
                string dbStatement = null;
                string httpMethodWithSpace = null;
                string httpUrl = null;
                string peerAddress = null;

                foreach (KeyValuePair<string, string> tag in currentActivity.Tags)
                {
                    // interpret Tags as defined by OpenTracing conventions
                    // https://github.com/opentracing/specification/blob/master/semantic_conventions.md
                    switch (tag.Key)
                    {
                        case "operation.name": // not defined by OpenTracing
                            {
                                telemetry.Name = tag.Value;
                                continue; // skip Properties
                            }
                        case "operation.data": // not defined by OpenTracing
                            {
                                telemetry.Data = tag.Value;
                                continue; // skip Properties
                            }
                        case "db.statement":
                            {
                                dbStatement = tag.Value;
                                break;
                            }
                        case "error":
                            {
                                bool failed;
                                if (bool.TryParse(tag.Value, out failed))
                                {
                                    telemetry.Success = !failed;
                                    continue; // skip Properties
                                }
                                break;
                            }
                        case "http.status_code":
                            {
                                telemetry.ResultCode = tag.Value;
                                continue; // skip Properties
                            }
                        case "http.method":
                            {
                                httpMethodWithSpace = tag.Value + " ";
                                break;
                            }
                        case "http.url":
                            {
                                httpUrl = tag.Value;
                                if (Uri.TryCreate(tag.Value, UriKind.RelativeOrAbsolute, out requestUri))
                                {
                                    continue; // skip Properties
                                }
                                break;
                            }
                        case "peer.address":
                            {
                                peerAddress = tag.Value;
                                break;
                            }
                        case "peer.hostname":
                            {
                                telemetry.Target = tag.Value;
                                continue; // skip Properties
                            }
                        case "peer.service":
                            {
                                telemetry.Type = tag.Value;
                                continue; // skip Properties
                            }
                    }

                    // if more than one tag with the same name is specified, the first one wins
                    if (!telemetry.Context.Properties.ContainsKey(tag.Key))
                    {
                        telemetry.Context.Properties[tag.Key] = tag.Value;
                    }
                }

                telemetry.Context.Properties["DiagnosticSource"] = this.diagnosticListener.Name;
                telemetry.Context.Properties["Activity"] = currentActivity.OperationName.Substring(0, currentActivity.OperationName.Length - StopSuffix.Length);

                if (string.IsNullOrEmpty(telemetry.Target))
                {
                    // 'peer.address' can be not user-friendly, thus use only if nothing else specified
                    telemetry.Target = requestUri?.Host ?? peerAddress;
                }

                if (string.IsNullOrEmpty(telemetry.Name))
                {
                    telemetry.Name = (httpMethodWithSpace + (httpUrl ?? requestUri?.OriginalString))
                        ?? currentActivity.OperationName;
                }

                if (string.IsNullOrEmpty(telemetry.Data))
                {
                    telemetry.Data = dbStatement ?? requestUri?.OriginalString ?? httpUrl;
                }

                this.client.Track(telemetry);
            }
        }
    }
}