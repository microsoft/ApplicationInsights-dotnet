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
        private readonly ExceptionTrackingTelemetryModule exceptionModule;

#if !NET40
        // Delegate preferred over Invoke to gain performance, only in NET45 or above as ISubscriptionToken is not available in Net40
        private Func<HttpResponse, Action<HttpContext>, ISubscriptionToken> openDelegateForInvokingAddOnSendingHeadersMethod;
#endif
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
                    else
                    {
                        if (module is ExceptionTrackingTelemetryModule)
                        {
                            this.exceptionModule = (ExceptionTrackingTelemetryModule)module;
                        }
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
#if !NET40
                    this.openDelegateForInvokingAddOnSendingHeadersMethod = this.CreateOpenDelegate(this.addOnSendingHeadersMethod);
#endif
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
#if NET40
                    context.EndRequest += this.OnEndRequest;
                    context.PreRequestHandlerExecute += this.OnPreRequestHandlerExecute;
#endif
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
                HttpApplication httpApplication = (HttpApplication)sender;

                this.TraceCallback("OnBegin", httpApplication);

                if (this.requestModule != null)
                {
                    if (this.requestModule.SetComponentCorrelationHttpHeaders)
                    {
                        this.AddCorreleationHeaderOnSendRequestHeaders(httpApplication);
                    }

#if NET40
                    this.requestModule.OnBeginRequest(httpApplication.Context);
#endif
                }
            }
        }

        /// <summary>
        /// When sending the response headers, allow request module to add the IKey's target hash.
        /// </summary>
        /// <param name="httpApplication">HttpApplication instance.</param>
        private void AddCorreleationHeaderOnSendRequestHeaders(HttpApplication httpApplication)
        {
            try
            {
                if (httpApplication != null && httpApplication.Response != null)
                {                                     
                    if (this.addOnSendingHeadersMethodExists)
                    {                        
#if !NET40
                        // Faster delegate based invocation.
                        this.openDelegateForInvokingAddOnSendingHeadersMethod.Invoke(httpApplication.Response, this.addOnSendingHeadersMethodParam);
#else
                        this.addOnSendingHeadersMethod.Invoke(httpApplication.Response, this.addOnSendingHeadersMethodParams);
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                WebEventSource.Log.HookAddOnSendingHeadersFailedWarning(ex.ToInvariantString());
            }
        }

#if !NET40
        /// <summary>
        /// Creates open delegate for faster invocation than regular Invoke.        
        /// </summary>
        /// <param name="mi">MethodInfo for which open delegate is to be created.</param>
        private Func<HttpResponse, Action<HttpContext>, ISubscriptionToken> CreateOpenDelegate(MethodInfo mi)
        {
            var openDelegate = Delegate.CreateDelegate(
                typeof(Func<HttpResponse, Action<HttpContext>, ISubscriptionToken>),
                null,
                mi,
                true);

            return (Func<HttpResponse, Action<HttpContext>, ISubscriptionToken>)openDelegate;
        }
#endif

#if NET40
        private void OnPreRequestHandlerExecute(object sender, EventArgs eventArgs)
        {
            if (this.isEnabled)
            {
                HttpApplication httpApplication = (HttpApplication)sender;

                this.TraceCallback("OnPreRequestHandlerExecute", httpApplication);

                this.requestModule?.OnPreRequestHandlerExecute(httpApplication.Context);
            }
        }

        private void OnEndRequest(object sender, EventArgs eventArgs)
        {
            if (this.isEnabled)
            {
                var httpApplication = (HttpApplication)sender;
                this.TraceCallback("OnEndRequest", httpApplication);

                if (this.IsFirstRequest(httpApplication))
                {
                    if (this.exceptionModule != null)
                    {
                        this.exceptionModule.OnError(httpApplication.Context);
                    }

                    if (this.requestModule != null)
                    {
                        this.requestModule.OnEndRequest(httpApplication.Context);
                    }
                }
                else
                {
                    WebEventSource.Log.RequestFiltered();
                }
            }
        }

        private bool IsFirstRequest(HttpApplication application)
        {
            var firstRequest = true;
            try
            {
                if (application.Context != null)
                {
                    firstRequest = application.Context.Items[RequestTrackingConstants.EndRequestCallFlag] == null;
                    if (firstRequest)
                    {
                        application.Context.Items.Add(RequestTrackingConstants.EndRequestCallFlag, true);
                    }
                }
            }
            catch (Exception exc)
            {
                WebEventSource.Log.FlagCheckFailure(exc.ToInvariantString());
            }

            return firstRequest;
        }
#endif

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
    }
}
