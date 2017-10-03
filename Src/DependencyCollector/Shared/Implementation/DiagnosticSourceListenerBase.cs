namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Base implementation of DiagnosticSource listener. 
    /// Takes care of managing subscriptions to multiple sources and their events.
    /// </summary>
    internal abstract class DiagnosticSourceListenerBase : DiagnosticSourceListenerBase<object>
    {
        protected DiagnosticSourceListenerBase(TelemetryConfiguration configuration) : base(configuration)
        {
        }
    }

    /// <summary>
    /// Base implementation of DiagnosticSource listener. 
    /// Takes care of managing subscriptions to multiple sources and their events.
    /// 
    /// <typeparamref name="TContext">The type of processing context for given diagnostic source.</typeparamref>
    /// </summary>
    internal abstract class DiagnosticSourceListenerBase<TContext> : IObserver<DiagnosticListener>, IDisposable
    {
        protected readonly TelemetryClient Client;
        protected readonly TelemetryConfiguration Configuration;

        private readonly IDisposable listenerSubscription;
        private List<IDisposable> individualSubscriptions;


        protected DiagnosticSourceListenerBase(TelemetryConfiguration configuration)
        {
            this.Configuration = configuration;
            this.Client = new TelemetryClient(configuration);

            try
            {
                this.listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.DiagnosticSourceListenerFailedToSubscribe(this.GetType().Name, ex.ToInvariantString());
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
        public virtual void OnNext(DiagnosticListener value)
        {
            if (value == null || !this.IsSourceEnabled(value))
            {
                return;
            }

            var individualListener = new IndividualDiagnosticSourceListener(value, this, this.GetListenerContext(value));
            IDisposable subscription = value.Subscribe(
                individualListener,
                (evnt, input1, input2) => this.IsEventEnabled(evnt, input1, input2, value, individualListener.context));

            if (this.individualSubscriptions == null)
            {
                this.individualSubscriptions = new List<IDisposable>();
            }
            this.individualSubscriptions.Add(subscription);
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
        /// Gets diagnostic source-specific context for processing events from that source.
        /// </summary>
        /// <param name="diagnosticListener">The diagnostic source.</param>
        /// <returns></returns>
        protected virtual TContext GetListenerContext(DiagnosticListener diagnosticListener)
        {
            return default(TContext);
        }

        /// <summary>
        /// Checks if the diagnostic source is enabled by this listener.
        /// </summary>
        /// <param name="diagnosticListener">The diagnostic source.</param>
        /// <returns></returns>
        internal abstract bool IsSourceEnabled(DiagnosticListener diagnosticListener);

        /// <summary>
        /// Checks if the event is enabled by this listener.
        /// </summary>
        /// <param name="evnt">The event name.</param>
        /// <param name="arg1">First event input object (<see cref="DiagnosticListener.IsEnabled(string, object, object)"/>).</param>
        /// <param name="arg2">Second event input object (<see cref="DiagnosticListener.IsEnabled(string, object, object)"/>).</param>
        /// <param name="diagnosticListener">The diagnostic source.</param>
        /// <param name="context">The diagnostic source-specific context (<see cref="GetListenerContext(DiagnosticListener)"/>).</param>
        /// <returns></returns>
        internal abstract bool IsEventEnabled(string evnt, object arg1, object arg2, DiagnosticListener diagnosticListener, TContext context);

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="evnt">The event (name-payload pair).</param>
        /// <param name="diagnosticListener">The diagnostic source.</param>
        /// <param name="context">The diagnostic source-specific context (<see cref="GetListenerContext(DiagnosticListener)"/>).</param>
        internal abstract void HandleEvent(KeyValuePair<string, object> evnt, DiagnosticListener diagnosticListener, TContext context);

        /// <summary>
        /// Event listener for a single Diagnostic Source.
        /// </summary>
        internal sealed class IndividualDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>
        {
            private readonly DiagnosticListener diagnosticListener;
            private readonly DiagnosticSourceListenerBase<TContext> telemetryDiagnosticSourceListener;
            internal readonly TContext context;

            internal IndividualDiagnosticSourceListener(DiagnosticListener diagnosticListener, DiagnosticSourceListenerBase<TContext> telemetryDiagnosticSourceListener, TContext context)
            {
                this.diagnosticListener = diagnosticListener;
                this.telemetryDiagnosticSourceListener = telemetryDiagnosticSourceListener;
                this.context = context;
            }

            public void OnNext(KeyValuePair<string, object> evnt)
            {
                this.telemetryDiagnosticSourceListener.HandleEvent(evnt, this.diagnosticListener, this.context);
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
        }
    }
}