//namespace Microsoft.AspNet.Mvc.Rendering
namespace Microsoft.ApplicationInsights.AspNet
{
	using Microsoft.ApplicationInsights;
	using Microsoft.AspNet.Mvc;
	using Microsoft.AspNet.Mvc.Rendering;
	using System.Collections.Generic;

	public static class ApplicationInsightsJavaScriptSnippetExtensions
	{
		public static HtmlString ApplicationInsightsJavaScriptSnippet(this IHtmlHelper helper, string instrumentationKey, Dictionary<string, string> parameters = null)
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