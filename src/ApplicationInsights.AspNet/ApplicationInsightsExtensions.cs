namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.ApplicationInsights.DataContracts;
	using Microsoft.ApplicationInsights.Extensibility;
	using Microsoft.AspNet.Builder;
	using Microsoft.Framework.ConfigurationModel;
	using Microsoft.Framework.DependencyInjection;
	using Microsoft.AspNet.Mvc.Rendering;
	using Microsoft.ApplicationInsights.AspNet.DataCollection;
	using Microsoft.ApplicationInsights.AspNet.Implementation;

	public static class ApplicationInsightsExtensions
	{
		public static IApplicationBuilder UseApplicationInsightsRequestTelemetry(this IApplicationBuilder app)
		{
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
			TelemetryConfiguration.Active.InstrumentationKey = config.Get("ApplicationInsights:InstrumentationKey");

			services.AddSingleton<TelemetryClient>((svcs) => {
				TelemetryConfiguration.Active.TelemetryInitializers.Add(new WebClientIpHeaderTelemetryInitializer(svcs));
				TelemetryConfiguration.Active.TelemetryInitializers.Add(new WebUserAgentTelemetryInitializer(svcs));
				return new TelemetryClient();
            });

			services.AddScoped<RequestTelemetry>((svcs) => {
				var rt = new RequestTelemetry();
				// this is workaround to inject proper instrumentation key into javascript:
				rt.Context.InstrumentationKey = svcs.GetService<TelemetryClient>().Context.InstrumentationKey;
				return rt;
			});

			services.AddScoped<HttpContextHolder>();
		}

		public static HtmlString ApplicationInsightsJavaScriptSnippet(this IHtmlHelper helper, string instrumentationKey)
		{
			//see: https://github.com/aspnet/Mvc/issues/2056
			//var client = (TelemetryClient)helper.ViewContext.HttpContext.ApplicationServices.GetService(typeof(TelemetryClient));
			return new HtmlString(@"<script language='javascript'> 
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
	}
}