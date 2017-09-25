namespace EmptyApp.FunctionalTests
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder();
            builder.AddApplicationInsightsSettings(instrumentationKey: "Foo", endpointAddress: "http://localhost:4001/v2/track/", developerMode: true);
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
