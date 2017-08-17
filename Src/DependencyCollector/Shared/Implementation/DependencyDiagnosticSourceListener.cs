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

    internal class DependencyDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private const string DataSourceNameSuffix = ".Monitoring";
        private const string ActivityNameSuffix = ".OutboundCall";
        private const string ActivityStopNameSuffix = ActivityNameSuffix + ".Stop";

        private readonly TelemetryClient client;
        private readonly TelemetryConfiguration configuration;
        private readonly DependencyDiagnosticSourceSubscriber subscriber;

        #region fetchers

        private readonly PropertyFetcher dependencyCallRequestUriFetcher = new PropertyFetcher("RequestUri");

        #endregion fetchers

        public DependencyDiagnosticSourceListener(TelemetryConfiguration configuration)
        {
            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceExtensibility + ":");

            this.configuration = configuration;

            this.subscriber = new DependencyDiagnosticSourceSubscriber(this);
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

        public void OnNext(KeyValuePair<string, object> evnt)
        {
            if (evnt.Key.EndsWith(ActivityStopNameSuffix, StringComparison.OrdinalIgnoreCase))
            {
                this.OnActivityStop(evnt.Value);
            }
        }

        public void Dispose()
        {
            if (this.subscriber != null)
            {
                this.subscriber.Dispose();
            }
        }

        /// <summary>
        /// Handler for Activity stop event (response is received for the outgoing request).
        /// </summary>
        internal void OnActivityStop(object payload)
        {
            Activity currentActivity = Activity.Current;
            if (currentActivity == null)
            {
                DependencyCollectorEventSource.Log.CurrentActivityIsNull();
                return;
            }

            // TODO define dedicated log event
            DependencyCollectorEventSource.Log.HttpCoreDiagnosticSourceListenerStop(currentActivity.Id);

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
            object uriObject = payload != null ? this.dependencyCallRequestUriFetcher.Fetch(payload) : null;
            Uri requestUri = uriObject as Uri;
            if (requestUri == null)
            {
                string uriString = uriObject as string;
                if (!string.IsNullOrEmpty(uriString))
                {
                    Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out requestUri);
                }
            }

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
                            // if no valid URI specified in the payload use this one
                            if (requestUri == null && !string.IsNullOrEmpty(tag.Value))
                            {
                                if (Uri.TryCreate(tag.Value, UriKind.RelativeOrAbsolute, out requestUri))
                                {
                                    continue; // skip Properties
                                }
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

            telemetry.Context.Properties["activity"] = currentActivity.OperationName;

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

        /// <summary>
        /// Diagnostic listener implementation that listens for events specific to outgoing dependency requests.
        /// </summary>
        private class DependencyDiagnosticSourceSubscriber : IObserver<DiagnosticListener>, IDisposable
        {
            private readonly DependencyDiagnosticSourceListener rddDiagnosticListener;
            private readonly IDisposable listenerSubscription;
            private List<IDisposable> eventSubscriptions;

            internal DependencyDiagnosticSourceSubscriber(DependencyDiagnosticSourceListener listener)
            {
                this.rddDiagnosticListener = listener;

                try
                {
                    this.listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
                }
                catch (Exception ex)
                {
                    // TODO define dedicated log event
                    DependencyCollectorEventSource.Log.HttpCoreDiagnosticSubscriberFailedToSubscribe(ex.ToInvariantString());
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
                    if (value.Name.EndsWith(DataSourceNameSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        IDisposable eventSubscription = value.Subscribe(
                            this.rddDiagnosticListener,
                            (evnt, r, _) => evnt.EndsWith(ActivityNameSuffix, StringComparison.OrdinalIgnoreCase)
                                || evnt.EndsWith(ActivityStopNameSuffix, StringComparison.OrdinalIgnoreCase));

                        if (this.eventSubscriptions == null)
                        {
                            this.eventSubscriptions = new List<IDisposable>();
                        }
                        this.eventSubscriptions.Add(eventSubscription);
                    }
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
                if (this.eventSubscriptions != null)
                {
                    foreach (var eventSubscription in this.eventSubscriptions)
                    {
                        eventSubscription.Dispose();
                    }
                }

                if (this.listenerSubscription != null)
                {
                    this.listenerSubscription.Dispose();
                }
            }
        }
    }
}