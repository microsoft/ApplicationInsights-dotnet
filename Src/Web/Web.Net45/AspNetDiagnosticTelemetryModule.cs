namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Web;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Listens to ASP.NET DiagnosticSource and enables instrumentation with Activity: let ASP.NET create root Activity for the request.
    /// </summary>
    public class AspNetDiagnosticTelemetryModule : IObserver<DiagnosticListener>, IDisposable, ITelemetryModule
    {
        private const string AspNetListenerName = "Microsoft.AspNet.TelemetryCorrelation";
        private const string IncomingRequestEventName = "Microsoft.AspNet.HttpReqIn";
        private const string IncomingRequestStartEventName = "Microsoft.AspNet.HttpReqIn.Start";
        private const string IncomingRequestStopEventName = "Microsoft.AspNet.HttpReqIn.Stop";
        private const string IncomingRequestStopLostActivity = "Microsoft.AspNet.HttpReqIn.ActivityLost.Stop";

        private IDisposable allListenerSubscription;
        private RequestTrackingTelemetryModule requestModule;
        private ExceptionTrackingTelemetryModule exceptionModule;

        private IDisposable aspNetSubscription;

        /// <summary>
        /// Indicates if module initialized successfully.
        /// </summary>
        private bool isEnabled = true;

        /// <summary>
        /// Initializes the telemetry module.
        /// </summary>
        /// <param name="configuration">Telemetry configuration to use for initialization.</param>
        public void Initialize(TelemetryConfiguration configuration)
        {
            try
            {
                foreach (var module in TelemetryModules.Instance.Modules)
                {
                    if (module is RequestTrackingTelemetryModule)
                    {
                        this.requestModule = (RequestTrackingTelemetryModule)module;
                    }
                    else if (module is ExceptionTrackingTelemetryModule)
                    {
                        this.exceptionModule = (ExceptionTrackingTelemetryModule)module;
                    }
                }
            }
            catch (Exception exc)
            {
                this.isEnabled = false;
                WebEventSource.Log.WebModuleInitializationExceptionEvent(exc.ToInvariantString());
            }

            this.allListenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
        }

        /// <summary>
        /// Implements IObserver OnNext callback, subscribes to AspNet DiagnosticSource.
        /// </summary>
        /// <param name="value">DiagnosticListener value.</param>
        public void OnNext(DiagnosticListener value)
        {
            if (this.isEnabled && value.Name == AspNetListenerName)
            {
                var eventListener = new AspNetEventObserver(this.requestModule, this.exceptionModule);
                this.aspNetSubscription = value.Subscribe(eventListener, eventListener.IsEnabled);
            }
        }

        /// <summary>
        /// Disposes all subscriptions to DiagnosticSources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        #region IObserver

        /// <summary>
        /// IObserver OnError callback.
        /// </summary>
        /// <param name="error">Exception instance.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// IObserver OnCompleted callback.
        /// </summary>
        public void OnCompleted()
        {
        }

        #endregion

        private void Dispose(bool dispose)
        {
            if (dispose)
            {
                this.aspNetSubscription?.Dispose();
                this.allListenerSubscription?.Dispose();
            }
        }

        private class AspNetEventObserver : IObserver<KeyValuePair<string, object>>
        {
            private const string FirstRequestFlag = "Microsoft.ApplicationInsights.FirstRequestFlag";
            private readonly RequestTrackingTelemetryModule requestModule;
            private readonly ExceptionTrackingTelemetryModule exceptionModule;

            public AspNetEventObserver(RequestTrackingTelemetryModule requestModule, ExceptionTrackingTelemetryModule exceptionModule)
            {
                this.requestModule = requestModule;
                this.exceptionModule = exceptionModule;
            }

            public Func<string, object, object, bool> IsEnabled => (name, activityObj, _) =>
            {
                if (name == IncomingRequestEventName)
                {
                    var activity = activityObj as Activity;
                    if (activity == null)
                    {
                        // this is a first IsEnabled call without context that ensures that Activity instrumentation is on
                        return true;
                    }

                    // ParentId is null, means that there was no Request-Id header, which means we have to look for AppInsights/custom headers
                    if (activity.ParentId == null)
                    {
                        var context = HttpContext.Current;
                        var request = context.Request;
                        string rootId = request.UnvalidatedGetHeader(ActivityHelpers.RootOperationIdHeaderName);
                        if (!string.IsNullOrEmpty(rootId))
                        {
                            // Got legacy headers from older AppInsights version or some custom header.
                            // Let's set activity ParentId with custom root id
                            activity.SetParentId(rootId);
                        }
                    }
                }

                return true;
            };

            public void OnNext(KeyValuePair<string, object> value)
            {
                var context = HttpContext.Current;

                if (value.Key == IncomingRequestStartEventName)
                {
                    this.requestModule?.OnBeginRequest(context);
                }
                else if (value.Key == IncomingRequestStopEventName || value.Key == IncomingRequestStopLostActivity)
                {
                    if (this.IsFirstRequest(context))
                    {
                        // Asp.Net Http Module detected that activity was lost, it notifies about it with this event
                        // It means that Activity was previously reported in BeginRequest and we saved it in HttpContext.Current
                        // we will use it in Web.OperationCorrealtionTelemetryInitializer to init exceptions and request
                        this.exceptionModule?.OnError(context);
                        this.requestModule?.OnEndRequest(context);
                    }
                }
            }

            #region IObserver

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }

            #endregion

            private bool IsFirstRequest(HttpContext context)
            {
                var firstRequest = true;
                try
                {
                    if (context != null)
                    {
                        firstRequest = context.Items[FirstRequestFlag] == null;
                        if (firstRequest)
                        {
                            context.Items.Add(FirstRequestFlag, true);
                        }
                    }
                }
                catch (Exception exc)
                {
                    WebEventSource.Log.FlagCheckFailure(exc.ToInvariantString());
                }

                return firstRequest;
            }
        }
    }
}
