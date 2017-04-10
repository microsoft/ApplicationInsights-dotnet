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
    /// Diagnostic listener implementation that listens for Http DiagnosticSource to see all outgoing HTTP dependency requests.
    /// </summary>
    internal class HttpDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly FrameworkHttpProcessing httpProcessingFramework;
        private HttpDiagnosticSourceSubscriber subscribeHelper;
        private bool disposed = false;

        internal HttpDiagnosticSourceListener(FrameworkHttpProcessing httpProcessing)
        {
            this.httpProcessingFramework = httpProcessing;
            this.subscribeHelper = new HttpDiagnosticSourceSubscriber(this);
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
        /// This method gets called once for each event from the Http DiagnosticSource.
        /// </summary>
        /// <param name="value">The pair containing the event name, and an object representing the payload. The payload
        /// is essentially a dynamic object that contain different properties depending on the event.</param>
        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                dynamic propertyBag = value.Value;                        
                switch (value.Key)
                {
                    case "System.Net.Http.Request":
                        this.httpProcessingFramework.OnRequestSend((HttpWebRequest)propertyBag.Request);
                        break;
                    case "System.Net.Http.Response":
                        this.httpProcessingFramework.OnResponseReceive((HttpWebRequest)propertyBag.Request, (HttpWebResponse)propertyBag.Response);
                        break;
                }
            }
            catch (Exception exc)
            {
                DependencyCollectorEventSource.Log.CallbackError(0, "OnNext", exc);
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
                    if (this.subscribeHelper != null)
                    {
                        this.subscribeHelper.Dispose();
                    }
                }

                this.disposed = true;
            }
        }
    }
}
#endif