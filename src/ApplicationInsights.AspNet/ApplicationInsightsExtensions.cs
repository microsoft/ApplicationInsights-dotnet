namespace Microsoft.ApplicationInsights.AspNet
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Mvc.Rendering;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;

    public static class ApplicationInsightsExtensions
    {
        public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
        {
            // TODO: Register if customer did not register
            app.UseRequestServices();

            app.UseMiddleware<ApplicationInsightsRequestMiddleware>();
            return app;
        }

        public static IApplicationBuilder UseApplicationInsightsExceptionTelemetry(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApplicationInsightsExceptionMiddleware>();
            return app;
        }

        public static IApplicationBuilder SetApplicationInsightsTelemetryDeveloperMode(this IApplicationBuilder app)
        {
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
            return app;
        }

        public static void AddApplicationInsightsTelemetry(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IContextInitializer, DomainNameRoleInstanceContextInitializer>();

            services.AddSingleton<ITelemetryInitializer, ClientIpHeaderTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationIdTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, OperationNameTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, UserAgentTelemetryInitializer>();

            services.AddSingleton<TelemetryConfiguration>(serviceProvider =>
            {
                var telemetryConfiguration = new TelemetryConfiguration();
                ActiveConfigurationManager.AddInstrumentationKey(telemetryConfiguration, config);
                AddServicesToCollection(serviceProvider, telemetryConfiguration.ContextInitializers);
                AddServicesToCollection(serviceProvider, telemetryConfiguration.TelemetryInitializers);
                return telemetryConfiguration;
            });

            services.AddScoped<TelemetryClient>();

            services.AddScoped<RequestTelemetry>((svcs) => {
                var rt = new RequestTelemetry();
                // this is workaround to inject proper instrumentation key into javascript:
                rt.Context.InstrumentationKey = svcs.GetService<TelemetryClient>().Context.InstrumentationKey;
                return rt;
            });
        }

        public static HtmlString ApplicationInsightsJavaScriptSnippet(this IHtmlHelper helper, string instrumentationKey)
        {
            HtmlString result = HtmlString.Empty;
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                result = new HtmlString(@"<script language='javascript'> 
                 var appInsights = window.appInsights || function(config){ 
                     function s(config){t[config]=function(){var i=arguments; t.queue.push(function(){ t[config].apply(t, i)})} 
                     } 
                     var t = { config:config }, r = document, f = window, e = ""script"", o = r.createElement(e), i, u;for(o.src=config.url||""//az416426.vo.msecnd.net/scripts/a/ai.0.js"",r.getElementsByTagName(e)[0].parentNode.appendChild(o),t.cookie=r.cookie,t.queue=[],i=[""Event"",""Exception"",""Metric"",""PageView"",""Trace""];i.length;)s(""track""+i.pop());return config.disableExceptionTracking||(i=""onerror"",s(""_""+i),u=f[i],f[i]=function(config, r, f, e, o) { var s = u && u(config, r, f, e, o); return s !== !0 && t[""_"" + i](config, r, f, e, o),s}),t 
                 }({ 
                     instrumentationKey:""" + instrumentationKey + @""" 
                 }); 
  
                 window.appInsights=appInsights; 
                 appInsights.trackPageView(); 
</script>");
            }
            else
            {
                // TODO: Diagnostics
            }
            return result;
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