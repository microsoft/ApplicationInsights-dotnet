namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Diagnostics;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

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
                    AspNetCoreEventSource.Instance.LogTelemetryInitializerBaseInitializeContextNull();
                    return;
                }

                if (context.RequestServices == null)
                {
                    AspNetCoreEventSource.Instance.LogTelemetryInitializerBaseInitializeRequestServicesNull();
                    return;
                }

                var request = context.RequestServices.GetService<RequestTelemetry>();

                if (request == null)
                {
                    AspNetCoreEventSource.Instance.LogTelemetryInitializerBaseInitializeRequestNull();
                    return;
                }

                this.OnInitializeTelemetry(context, request, telemetry);
            }
            catch (Exception exp)
            {
                AspNetCoreEventSource.Instance.LogTelemetryInitializerBaseInitializeException(exp.ToString());
                Debug.WriteLine(exp);
            }
        }

        protected abstract void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry);
    }
}