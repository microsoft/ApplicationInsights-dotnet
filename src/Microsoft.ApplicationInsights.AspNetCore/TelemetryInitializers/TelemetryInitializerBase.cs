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
        private readonly IHttpContextAccessor httpContextAccessor;

        public TelemetryInitializerBase(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Initialize(ITelemetry telemetry)
        {
            var context = this.httpContextAccessor.HttpContext;

            if (context == null)
            {
                AspNetCoreEventSource.Instance.LogTelemetryInitializerBaseInitializeContextNull();
                return;
            }

            lock (context)
            {
                var request = context.Features.Get<RequestTelemetry>();

                if (request == null)
                {
                    AspNetCoreEventSource.Instance.LogTelemetryInitializerBaseInitializeRequestNull();
                    return;
                }

                this.OnInitializeTelemetry(context, request, telemetry);
            }
        }

        protected abstract void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry);
    }
}