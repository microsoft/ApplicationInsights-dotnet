namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Http;
    using Microsoft.Framework.DependencyInjection;
    using System;
    using System.Diagnostics;

    public abstract class TelemetryInitializerBase : ITelemetryInitializer
    {
        private IServiceProvider serviceProvider;

        public TelemetryInitializerBase(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            if (this.serviceProvider == null)
            {
                // TODO: Diagnostics
            }
        }

        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                if (serviceProvider == null)
                {
                    return;
                }

                var contextHolder = this.serviceProvider.GetService<HttpContextHolder>();
                
                if (contextHolder == null)
                {
                    //TODO: Diagnostics!
                    return;
                }

                var context = contextHolder.Context;

                if (context == null)
                {
                    //TODO: Diagnostics!
                    return;
                }

                var request = this.serviceProvider.GetService<RequestTelemetry>();

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