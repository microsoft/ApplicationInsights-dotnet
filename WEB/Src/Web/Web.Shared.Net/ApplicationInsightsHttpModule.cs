namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Reflection;
    using System.Web;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.Implementation;

#pragma warning disable 0612

    /// <summary>
    /// Platform agnostic module for web application instrumentation.
    /// </summary>
    public sealed class ApplicationInsightsHttpModule : IHttpModule
    {
        private readonly RequestTrackingTelemetryModule requestModule;

        // Delegate preferred over Invoke to gain performance, only in NET45 or above as ISubscriptionToken is not available in Net40
        private Func<HttpResponse, Action<HttpContext>, ISubscriptionToken> openDelegateForInvokingAddOnSendingHeadersMethod;
        private MethodInfo addOnSendingHeadersMethod;                
        private bool addOnSendingHeadersMethodExists;
        private Action<HttpContext> addOnSendingHeadersMethodParam;
        private object[] addOnSendingHeadersMethodParams;

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
                foreach (var module in TelemetryModules.Instance.Modules)
                {
                    if (module is RequestTrackingTelemetryModule)
                    {
                        this.requestModule = (RequestTrackingTelemetryModule)module;
                    }
                }
                
                // We use reflection here because 'AddOnSendingHeaders' is only available post .net framework 4.5.2. Hence we call it if we can find it.
                // Not using reflection would result in MissingMethodException when 4.5 or 4.5.1 is present. 
                this.addOnSendingHeadersMethod = typeof(HttpResponse).GetMethod("AddOnSendingHeaders");
                this.addOnSendingHeadersMethodExists = this.addOnSendingHeadersMethod != null;

                if (this.addOnSendingHeadersMethodExists)
                {
                    this.addOnSendingHeadersMethodParam = new Action<HttpContext>((httpContext) =>
                            {
                                try
                                {
                                    if (this.requestModule != null)
                                    {
                                        this.requestModule.AddTargetHashForResponseHeader(httpContext);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    WebEventSource.Log.AddTargetHeaderFailedWarning(ex.ToInvariantString());
                                }
                            });
                    this.addOnSendingHeadersMethodParams = new object[] { this.addOnSendingHeadersMethodParam };
                    this.openDelegateForInvokingAddOnSendingHeadersMethod = CreateOpenDelegate(this.addOnSendingHeadersMethod);
                }
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

        private static void TraceCallback(string callback, HttpApplication application)
        {
            if (WebEventSource.IsVerboseEnabled)
            {
                try
                {
                    if (application.Context != null)
                    {
                        // Url.ToString internally builds local member once and then always returns it
                        // During serialization we will anyway call same ToString() so we do not force unnecessary formatting just for tracing 
                        var url = application.Context.Request.UnvalidatedGetUrl();
                        string logUrl = (url != null) ? url.ToString() : string.Empty;

                        WebEventSource.Log.WebModuleCallback(callback, logUrl);
                    }
                }
                catch (Exception exc)
                {
                    WebEventSource.Log.TraceCallbackFailure(callback, exc.ToInvariantString());
                }
            }
        }

        /// <summary>
        /// Creates open delegate for faster invocation than regular Invoke.        
        /// </summary>
        /// <param name="mi">MethodInfo for which open delegate is to be created.</param>
        private static Func<HttpResponse, Action<HttpContext>, ISubscriptionToken> CreateOpenDelegate(MethodInfo mi)
        {
            var openDelegate = Delegate.CreateDelegate(
                typeof(Func<HttpResponse, Action<HttpContext>, ISubscriptionToken>),
                null,
                mi,
                true);

            return (Func<HttpResponse, Action<HttpContext>, ISubscriptionToken>)openDelegate;
        }

        private void OnBeginRequest(object sender, EventArgs eventArgs)
        {
            if (this.isEnabled)
            {
                HttpApplication httpApplication = (HttpApplication)sender;

                TraceCallback("OnBegin", httpApplication);

                if (this.requestModule != null)
                {
                    if (this.requestModule.SetComponentCorrelationHttpHeaders)
                    {
                        this.AddCorrelationHeaderOnSendRequestHeaders(httpApplication);
                    }
                }
            }
        }

        /// <summary>
        /// When sending the response headers, allow request module to add the IKey's target hash.
        /// </summary>
        /// <param name="httpApplication">HttpApplication instance.</param>
        private void AddCorrelationHeaderOnSendRequestHeaders(HttpApplication httpApplication)
        {
            try
            {
                if (httpApplication != null && httpApplication.Response != null)
                {                                     
                    if (this.addOnSendingHeadersMethodExists)
                    {                        
                        // Faster delegate based invocation.
                        this.openDelegateForInvokingAddOnSendingHeadersMethod.Invoke(httpApplication.Response, this.addOnSendingHeadersMethodParam);
                    }
                }
            }
            catch (Exception ex)
            {
                WebEventSource.Log.HookAddOnSendingHeadersFailedWarning(ex.ToInvariantString());
            }
        }
    }
}
