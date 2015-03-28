namespace Microsoft.ApplicationInsights.AspNet.DataCollection
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
        IServiceProvider serviceProvider;

        public TelemetryInitializerBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            this.serviceProvider = serviceProvider;
        }

        public void Initialize(ITelemetry telemetry)
        {
            try
            {
                var context = this.serviceProvider.GetService<HttpContextHolder>().Context;

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