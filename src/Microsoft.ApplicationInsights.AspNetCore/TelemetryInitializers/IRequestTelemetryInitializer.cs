using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    interface IRequestTelemetryInitializer
    {
        void Initialize(HttpContext context, RequestTelemetry requestTelemetry);
    }

    internal class ApplicationInsightsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var appInsightsInitializer = app.ApplicationServices.GetService<ApplicationInsightInitializer>();
                appInsightsInitializer.Start();
                next(app);
            };
        }
    }

    internal class ApplicationInsightInitializer: IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions;
        private readonly ILoggerFactory _loggerFactory;
        private readonly TelemetryClient _telemetryClient;
        private readonly IEnumerable<IApplicationInsightDiagnosticListener> _diagnosticListeners;

        public ApplicationInsightInitializer(
            ILoggerFactory loggerFactory,
            TelemetryClient telemetryClient,
            IEnumerable<IApplicationInsightDiagnosticListener> diagnosticListeners)
        {
            _loggerFactory = loggerFactory;
            _telemetryClient = telemetryClient;
            _diagnosticListeners = diagnosticListeners;

            _subscriptions = new List<IDisposable>();
        }

        public void Start()
        {
            DiagnosticListener.AllListeners.Subscribe(this);
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            foreach (var applicationInsightDiagnosticListener in _diagnosticListeners)
            {
                if (applicationInsightDiagnosticListener.ListenerName == value.Name)
                {
                    _subscriptions.Add(value.SubscribeWithAdapter(applicationInsightDiagnosticListener));
                }
            }
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
        }

        void IObserver<DiagnosticListener>.OnCompleted()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    internal interface IApplicationInsightDiagnosticListener
    {
        string ListenerName { get; }
    }

    public class AspNetCoreHostingListener: IApplicationInsightDiagnosticListener
    {
        private readonly TelemetryClient _client;
        private readonly ContextData<long> _beginRequestTimestamp = new ContextData<long>();
        private readonly string _sdkVersion;

        public AspNetCoreHostingListener(TelemetryClient client)
        {
            _client = client;
            _sdkVersion = SdkVersionUtils.VersionPrefix + SdkVersionUtils.GetAssemblyVersion();
        }

        public string ListenerName { get; } = "Microsoft.AspNetCore";

        [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
        public void OnBeginRequest(HttpContext httpContext, long timestamp)
        {
            httpContext.Features.Set(new RequestTelemetry());

            _beginRequestTimestamp.Value = timestamp;
            _client.Context.Operation.Id = httpContext.TraceIdentifier;
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.EndRequest")]
        public void OnEndRequest(HttpContext httpContext, long timestamp)
        {
            var telemetry = httpContext.Features.Get<RequestTelemetry>();

            telemetry.Duration = new TimeSpan(timestamp - _beginRequestTimestamp.Value);
            telemetry.Timestamp = DateTime.Now - telemetry.Duration;
            telemetry.ResponseCode = httpContext.Response.StatusCode.ToString();
            telemetry.Success =  (httpContext.Response.StatusCode < 400);
            telemetry.HttpMethod = httpContext.Request.Method;
            telemetry.Url = httpContext.Request.GetUri();
            telemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;
            _client.TrackRequest(telemetry);
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnHostingException(HttpContext httpContext, Exception exception)
        {
            OnException(httpContext, exception);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.HandledException")]
        public void OnDiagnosticsHandledException(HttpContext httpContext, Exception exception)
        {
            OnException(httpContext, exception);
        }

        [DiagnosticName("Microsoft.AspNetCore.Diagnostics.UnhandledException")]
        public void OnDiagnosticsUnhandledException(HttpContext httpContext, Exception exception)
        {
            OnException(httpContext, exception);
        }

        private void OnException(HttpContext context, Exception exception)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception);
            exceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
            exceptionTelemetry.Context.GetInternalContext().SdkVersion = _sdkVersion;
            _client.Track(exceptionTelemetry);
        }
    }
}
