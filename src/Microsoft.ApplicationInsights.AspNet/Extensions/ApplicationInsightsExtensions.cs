namespace Microsoft.Framework.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNet;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Mvc.Rendering;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.ApplicationInsights.AspNet.JavaScript;

    public static class ApplicationInsightsExtensions
    {
        public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestTrackingMiddleware>();
        }

        public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionTrackingMiddleware>();
        }

        public static IApplicationBuilder SetApplicationInsightsTelemetryDeveloperMode(this IApplicationBuilder app)
        {
            var telemetryConfiguration = app.ApplicationServices.GetRequiredService<TelemetryConfiguration>();
            telemetryConfiguration.TelemetryChannel.DeveloperMode = true;
            return app;
        }

        public static void AddApplicationInsightsTelemetry(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IContextInitializer, DomainNameRoleInstanceContextInitializer>();

            services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationIdTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, UserAgentTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebSessionTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, WebUserTelemetryInitializer>();

            services.AddSingleton<TelemetryConfiguration>(serviceProvider =>
            {
                var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                AddInstrumentationKey(config, telemetryConfiguration);
                telemetryConfiguration.TelemetryChannel = serviceProvider.GetService<ITelemetryChannel>() ?? telemetryConfiguration.TelemetryChannel;
                AddServicesToCollection(serviceProvider, telemetryConfiguration.ContextInitializers);
                AddServicesToCollection(serviceProvider, telemetryConfiguration.TelemetryInitializers);
                return telemetryConfiguration;
            });

            services.AddSingleton<IJavaScriptSnippet, ApplicationInsightsJavaScript>();

            services.AddScoped<TelemetryClient>();

            services.AddScoped<RequestTelemetry>((svcs) => {
                var rt = new RequestTelemetry();
                return rt;
            });
        }

        private static void AddInstrumentationKey(IConfiguration config, TelemetryConfiguration telemetryConfiguration)
        {
            // Read from configuration
            // Config.json will look like this:
            //
            //      "ApplicationInsights": {
            //            "InstrumentationKey": "11111111-2222-3333-4444-555555555555"
            //      }
            string instrumentationKey = config.Get("ApplicationInsights:InstrumentationKey");
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                telemetryConfiguration.InstrumentationKey = instrumentationKey;
            }
        }

        private static void AddServicesToCollection<T>(IServiceProvider serviceProvider, ICollection<T> collection)
        {
            var services = serviceProvider.GetService<IEnumerable<T>>();
            foreach (T service in services)
            {
                collection.Add(service);
            }
        }
    }
}