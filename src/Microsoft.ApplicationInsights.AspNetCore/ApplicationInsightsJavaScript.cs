namespace Microsoft.ApplicationInsights.AspNetCore
{
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// This class helps to inject Application Insights JavaScript snippet into applicaiton code.
    /// </summary>
    public class JavaScriptSnippet
    {
        /// <summary>
        /// Prefix of the code snippet. Use it if you need to initialize additional properties in javascript configuration.
        /// Usage:
        ///     ApplicationInsightsJavaScript.ScriptPrefix + @"{
        ///         instrumentationKey: 'key', 
        ///         property: 'test'
        ///     }" + ApplicationInsightsJavaScript.ScriptPostfix
        /// </summary>
        public static readonly string ScriptPrefix = @"<script type=""text/javascript"">
  var appInsights = window.appInsights || function(config){
  function s(config){
    t[config] = function(){ var i = arguments; t.queue.push(function(){ t[config].apply(t, i)})}
  }
  var t = { config:config }, r = document, f = window, e = ""script"", o = r.createElement(e), i, u; for (o.src = config.url || ""//az416426.vo.msecnd.net/scripts/a/ai.0.js"",r.getElementsByTagName(e)[0].parentNode.appendChild(o),t.cookie=r.cookie,t.queue=[],i=[""Event"",""Exception"",""Metric"",""PageView"",""Trace""];i.length;)s(""track""+i.pop());return config.disableExceptionTracking||(i=""onerror"",s(""_""+i),u=f[i],f[i]=function(config, r, f, e, o) { var s = u && u(config, r, f, e, o); return s !== !0 && t[""_"" + i](config, r, f, e, o),s}),t 
  }(";

        /// <summary>
        /// Postfix of the code snippet. See ScriptPrefix for details of usage.
        /// </summary>
        public static readonly string ScriptPostfix = @"); 
  window.appInsights = appInsights;
  appInsights.trackPageView();
</script> ";

        private TelemetryConfiguration telemetryConfiguration;

        public JavaScriptSnippet(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryConfiguration = telemetryConfiguration;
        }

        /// <summary>
        /// Returns code snippet with instrumentation key initialized from TelemetryConfiguration.
        /// </summary>
        /// <returns>JavaScript code snippet with instrumentation key or empty if instrumentation key was not set for the applicaiton.</returns>
        public string FullScript
        {
            get
            {
                if (!string.IsNullOrEmpty(this.telemetryConfiguration.InstrumentationKey))
                {
                    return ScriptPrefix + @"{
  instrumentationKey: '" + this.telemetryConfiguration.InstrumentationKey + @"'
}" + ScriptPostfix;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
