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
			return new HtmlString("<script language='javascript'>alert('Key: ' + '" + instrumentationKey + "');</script>");
		}
	}
}