namespace EmptyApp.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.ApplicationInsights.Channel;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.AspNetCore.Mvc.Infrastructure;

    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<ITelemetryChannel>(new BackTelemetryChannel());

            var builder = new ConfigurationBuilder();
            builder.AddApplicationInsightsSettings(instrumentationKey: "Foo");
            services.AddApplicationInsightsTelemetry(builder.Build());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseApplicationInsightsRequestTelemetry();
            app.UseDeveloperExceptionPage();
            app.UseApplicationInsightsExceptionTelemetry();

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
