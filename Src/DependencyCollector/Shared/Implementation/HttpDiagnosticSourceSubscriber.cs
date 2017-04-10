#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// A helper subscriber class helping the parent object, which is a HttpDiagnosticSourceListener, to subscribe
    /// to the Http DiagnosticSource. That way the parent object can subscribe to the DiagnosticSource without worry
    /// about the details around subscription.
    /// </summary>
    internal class HttpDiagnosticSourceSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private HttpDiagnosticSourceListener parent;
        private IDisposable allListenersSubscription;
        private IDisposable sourceSubscription;
        private bool disposed = false;

        internal HttpDiagnosticSourceSubscriber(HttpDiagnosticSourceListener parent)
        {
            GC.KeepAlive(HttpHandlerDiagnosticListener.SingletonInstance);
            this.parent = parent;
            this.allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This method gets called once for each existing DiagnosticListener when this
        /// DiagnosticListener is added to the list of DiagnosticListeners
        /// (<see cref="System.Diagnostics.DiagnosticListener.AllListeners"/>). This method
        /// will also be called for each subsequent DiagnosticListener that is added to
        /// the list of DiagnosticListeners.
        /// <seealso cref="IObserver{T}.OnNext(T)"/>
        /// </summary>
        /// <param name="value">The DiagnosticListener that exists when this listener was added to
        /// the list, or a DiagnosticListener that got added after this listener was added.</param>
        public void OnNext(DiagnosticListener value)
        {
            if (value != null)
            {
                if (value.Name == "System.Net.Http.Desktop")
                {
                    this.sourceSubscription = value.Subscribe(this.parent, (Predicate<string>)null);
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

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        /// <param name="disposing">The method has been called directly or indirectly by a user's code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.sourceSubscription != null)
                    {
                        this.sourceSubscription.Dispose();
                    }

                    if (this.allListenersSubscription != null)
                    {
                        this.allListenersSubscription.Dispose();
                    }
                }

                this.disposed = true;
            }
        }
    }
}
#endif