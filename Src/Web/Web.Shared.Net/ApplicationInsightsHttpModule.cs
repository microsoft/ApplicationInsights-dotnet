namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Threading;
    using System.Web;
    
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.Implementation;
    
    /// <summary>
    /// Platform agnostic module for web application instrumentation.
    /// </summary>
    public sealed class ApplicationInsightsHttpModule : IHttpModule
    {
        /// <summary>
        /// Indicates if module initialized successfully.
        /// </summary>
        private bool isEnabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsHttpModule"/> class.
        /// </summary>
        public ApplicationInsightsHttpModule()
        {
            try
            {
                // The call initializes TelemetryConfiguration that will create and Intialize modules
                TelemetryConfiguration configuration = TelemetryConfiguration.Active;
            }
            catch (Exception exc)
            {
                this.isEnabled = false;
                WebEventSource.Log.WebModuleInitializationExceptionEvent(exc.ToInvariantString());
            }
        }

        /// <summary>
        /// Initializes module for a given application.
        /// </summary>
        /// <param name="context">HttpApplication instance.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Context cannot be null")]
        public void Init(HttpApplication context)
        {
            if (this.isEnabled)
            {
                try
                {
                    context.BeginRequest += this.OnBeginRequest;
                    context.EndRequest += this.OnEndRequest;
                }
                catch (Exception exc)
                {
                    this.isEnabled = false;
                    WebEventSource.Log.WebModuleInitializationExceptionEvent(exc.ToInvariantString());
                }
            }
        }

        /// <summary>
        /// Required IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
        }

        private void OnBeginRequest(object sender, EventArgs eventArgs)
        {
            if (this.isEnabled)
            {
                this.TraceCallback("OnBegin", (HttpApplication)sender);
                WebEventsPublisher.Log.OnBegin();    
            }
        }

        private void OnEndRequest(object sender, EventArgs eventArgs)
        {
            if (this.isEnabled)
            {
                this.TraceCallback("OnEndRequest", (HttpApplication)sender);
                WebEventsPublisher.Log.OnError();
                WebEventsPublisher.Log.OnEnd();
            }
        }

        private void TraceCallback(string callback, HttpApplication application)
        {
            if (WebEventSource.Log.IsVerboseEnabled)
            {
                try
                {
                    if (application.Context != null)
                    {
                        // Url.ToString internally builds local member once and then always returns it
                        // During serialization we will anyway call same ToString() so we do not force unnesesary formatting just for tracing 
                        var url = application.Context.Request.UnvalidatedGetUrl();
                        string logUrl = (null != url) ? url.ToString() : string.Empty;

                        WebEventSource.Log.WebModuleCallback(callback, logUrl);
                    }
                }
                catch (Exception exc)
                {
                    WebEventSource.Log.TraceCallbackFailure(callback, exc.ToInvariantString());
                }
            }
        }
    }
}
