using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Middleware
{
    public static class ApplicationInsightsExtensions
    {
        public static void AddTelemetryClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddScoped(sp => RegisterClient(sp, configuration));
        }

        public static void UseApplicationInsightsForRequests(this IApplicationBuilder app)
        {
            app.UseMiddleware<AppInsightsRequestMiddleware>();
        }

        public static void UseApplicationInsightsForExceptions(this IApplicationBuilder app)
        {
            app.UseMiddleware<AppInsightsExceptionMiddleware>();
        }

        private static TelemetryClient RegisterClient(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            TelemetryClient client = null;
            try
            {
                string key = configuration.Get("InstrumentationKey");

                if (string.IsNullOrEmpty(key))
                {
                    // TODO; check logger for null
                    serviceProvider.GetService<ILogger>().WriteError("InstrumentationKey not registered");
                    return null;
                }

                var aiConfig = new TelemetryConfiguration();
                aiConfig.InstrumentationKey = key;
                var channel = new Channel.InProcessTelemetryChannel();
                aiConfig.TelemetryChannel = channel;

                var env = serviceProvider.GetService<IHostingEnvironment>();

                if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    aiConfig.TelemetryChannel.DeveloperMode = true;
                }

                client = new TelemetryClient(aiConfig);
                channel.Initialize(aiConfig);
            }
            catch (Exception e)
            {
                serviceProvider.GetService<ILogger>().WriteError(e.ToString());
            }

            return client;
        }

        private class AppInsightsRequestMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly IServiceProvider _services;

            public AppInsightsRequestMiddleware(RequestDelegate next, IServiceProvider services)
            {
                _services = services;
                _next = next;
            }

            public async Task Invoke(HttpContext httpContext)
            {
                using (var container = RequestServicesContainer.EnsureRequestServices(httpContext, _services))
                {
                    var client = httpContext.RequestServices.GetService<TelemetryClient>();

                    if (client == null)
                    {
                        _services.GetService<ILogger>().WriteError("AI TelemetryClient is not registered.");
                    }

                    var now = DateTime.UtcNow;

                    try
                    {
                        await _next.Invoke(httpContext);
                    }
                    finally
                    {
                        if (client != null)
                        {
                            var telemetry = new RequestTelemetry(
                                httpContext.Request.Method + " " + httpContext.Request.Path.Value,
                                now,
                                DateTime.UtcNow - now,
                                httpContext.Response.StatusCode.ToString(),
                                httpContext.Response.StatusCode < 400);

                            client.TrackRequest(telemetry);
                        }
                    }
                }
            }
        }

        private class AppInsightsExceptionMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly IServiceProvider _services;

            public AppInsightsExceptionMiddleware(RequestDelegate next, IServiceProvider services)
            {
                _services = services;
                _next = next;
            }

            public async Task Invoke(HttpContext httpContext)
            {
                using (var container = RequestServicesContainer.EnsureRequestServices(httpContext, _services))
                {
                    var client = httpContext.RequestServices.GetService<TelemetryClient>();

                    if (client == null)
                    {
                        _services.GetService<ILogger>().WriteWarning("AI TelemetryClient is not registered.");
                    }

                    try
                    {
                        await _next.Invoke(httpContext);
                    }
                    catch (Exception exp)
                    {
                        if (client != null)
                        {
                            client.TrackException(exp);
                        }

                        throw;
                    }
                }
            }
        }
    }
}
