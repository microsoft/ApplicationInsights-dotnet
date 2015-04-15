namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.Framework.DependencyInjection;
    using System;
    using System.Diagnostics;

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
                    //TODO: Diagnostics!
                    return;
                }

                if (context.RequestServices == null)
                {
                    //TODO: Diagnostics!
                    return;
                }

                var request = context.RequestServices.GetService<RequestTelemetry>();

                if (request == null)
                {
                    //TODO: Diagnostics!
                    return;
                }

                this.OnInitializeTelemetry(context, request, telemetry);
            }
            catch (Exception exp)
            {
                //TODO: Diagnostics!
                Debug.WriteLine(exp);
            }
        }

        protected abstract void OnInitializeTelemetry(
            HttpContext platformContext,
            RequestTelemetry requestTelemetry,
            ITelemetry telemetry);
    }
}