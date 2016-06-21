namespace Microsoft.AspNetCore.Mvc.Rendering
{
    using AspNetCore.Mvc.Rendering;
    using Html;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class ApplicationInsightsJavaScriptExtensions
    {
        /// <summary>
        /// Extension method to inject Application Insights JavaScript snippet into cshml files. 
        /// </summary>
        /// <param name="helper">Html helper object to align with razor code style.</param>
        /// <param name="configuration">Telemetry configuraiton to initialize snippet.</param>
        /// <returns>JavaScript snippt to insert into html page.</returns>
        public static HtmlString ApplicationInsightsJavaScript(this IHtmlHelper helper, TelemetryConfiguration configuration)
        {
            JavaScriptSnippet snippet = new JavaScriptSnippet(configuration);
            return new HtmlString(snippet.FullScript);
        }
    }
}
