namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Diagnostics;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public abstract class TelemetryInitializerBase : ITelemetryInitializer
    {
        private IHttpContextAccessor httpContextAccessor;

        public TelemetryInitializerBase(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException("httpContextAccessor");
            }

            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                var context = this.httpContextAccessor.HttpContext;

                if (context == null)
                {
                    TelemetryLogger.Instance.LogVerbose("TelemetryInitializerBase.Initialize - httpContextAccessor.HttpContext is null, returning.");
                    return;
                }

                if (context.RequestServices == null)
                {
                    TelemetryLogger.Instance.LogVerbose("TelemetryInitializerBase.Initialize - context.RequestServices is null, returning.");
                    return;
                }

                var request = context.RequestServices.GetService<RequestTelemetry>();

                if (request == null)
                {
                    TelemetryLogger.Instance.LogVerbose("TelemetryInitializerBase.Initialize - request is null, returning.");
                    return;
                }

                this.OnInitializeTelemetry(context, request, telemetry);
            }
            catch (Exception exp)
            {
                TelemetryLogger.Instance.LogError("TelemetryInitializerBase.Initialize error.", exp);
                Debug.WriteLine(exp);
            }
        }

        protected abstract void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry);
    }
}