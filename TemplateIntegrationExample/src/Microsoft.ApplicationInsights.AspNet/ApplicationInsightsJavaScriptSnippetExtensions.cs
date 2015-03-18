namespace Microsoft.AspNet.Mvc.Rendering
{
	using Microsoft.ApplicationInsights;
	using Microsoft.AspNet.Mvc;

	public static class ApplicationInsightsJavaScriptSnippetExtensions
    {
		public static HtmlString ApplicationInsightsJavaScriptSnippet(this IHtmlHelper helper)
		{
			//see: https://github.com/aspnet/Mvc/issues/2056
			var client = (TelemetryClient)helper.ViewContext.HttpContext.ApplicationServices.GetService(typeof(TelemetryClient));
			return new HtmlString("<script language='javascript'>alert('Key: ' + '" + client.Context.InstrumentationKey + "');</script>");
		}
	}
}