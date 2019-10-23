namespace EmptyApp.FunctionalTests
{
    using System;

    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
   
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var endpointAddress = new EndpointAddress();
            services.AddSingleton<EndpointAddress>(endpointAddress);

            var builder = new ConfigurationBuilder();
            builder.AddApplicationInsightsSettings(instrumentationKey: InProcessServer.IKey, endpointAddress: endpointAddress.ConnectionString, developerMode: true);
            services.AddSingleton(typeof(ITelemetryChannel), new InMemoryChannel());
            services.AddApplicationInsightsTelemetry(builder.Build());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.Use(next =>
            {
                return async context =>
                {
                    if (context.Request.GetUri().ToString().Contains("Exception"))
                    {
                        throw new InvalidOperationException();
                    }
                    else if (context.Request.GetUri().PathAndQuery == "/")
                    {
                        await context.Response.WriteAsync("Hello!");
                    }
                    else if (context.Request.GetUri().ToString().Contains("Mixed"))
                    {
                        TelemetryClient telemetryClient = (TelemetryClient)context.RequestServices.GetService(typeof(TelemetryClient));
                        telemetryClient.TrackEvent("GetContact");
                        telemetryClient.TrackMetric("ContactFile", 1);
                        telemetryClient.TrackTrace("Fetched contact details.", SeverityLevel.Information);
                        await context.Response.WriteAsync("Hello!");
                    }
                    else
                    {
                        await next(context);
                    }
                };
            });
        }
    }
}
