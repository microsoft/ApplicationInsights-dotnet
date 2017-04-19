#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Diagnostic listener implementation that listens for Http DiagnosticSource to see all outgoing HTTP dependency requests.
    /// </summary>
    internal class HttpDesktopDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly FrameworkHttpProcessing httpProcessingFramework;
        private readonly HttpDesktopDiagnosticSourceSubscriber subscribeHelper;
        private readonly PropertyFetcher requestFetcherRequestEvent;
        private readonly PropertyFetcher requestFetcherResponseEvent;
        private readonly PropertyFetcher responseFetcher;
        private bool disposed = false;

        internal HttpDesktopDiagnosticSourceListener(FrameworkHttpProcessing httpProcessing)
        {
            this.httpProcessingFramework = httpProcessing;
            this.subscribeHelper = new HttpDesktopDiagnosticSourceSubscriber(this);
            this.requestFetcherRequestEvent = new PropertyFetcher("Request");
            this.requestFetcherResponseEvent = new PropertyFetcher("Request");
            this.responseFetcher = new PropertyFetcher("Response");
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
                switch (value.Key)
                {
                    case "System.Net.Http.Desktop.HttpRequestOut.Start":

                    // remove "System.Net.Http.Request" in 2.5.0 (but keep the same code for "System.Net.Http.Desktop.HttpRequestOut.Start")
                    // event was temporarily introduced in DiagnosticSource and removed before stable release
                    case "System.Net.Http.Request": 
                    {
                        var request = (HttpWebRequest)this.requestFetcherRequestEvent.Fetch(value.Value);
                        this.httpProcessingFramework.OnRequestSend(request);
                        break;
                    }

                    case "System.Net.Http.Desktop.HttpRequestOut.Stop":

                    // remove "System.Net.Http.Response" in 2.5.0 (but keep the same code for "System.Net.Http.Desktop.HttpRequestOut.Stop")
                    // event was temporarily introduced in DiagnosticSource and removed before stable release
                    case "System.Net.Http.Response": 
                    {
                        var request = (HttpWebRequest)this.requestFetcherResponseEvent.Fetch(value.Value);
                        var response = (HttpWebResponse)this.responseFetcher.Fetch(value.Value);
                        this.httpProcessingFramework.OnResponseReceive(request, response);
                        break;
                    }
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