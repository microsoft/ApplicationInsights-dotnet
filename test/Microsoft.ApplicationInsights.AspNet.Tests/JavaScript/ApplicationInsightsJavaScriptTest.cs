namespace Microsoft.Framework.DependencyInjection
{
    using Microsoft.ApplicationInsights.AspNet.JavaScript;
    using Microsoft.ApplicationInsights.Extensibility;
    using Xunit;

    public static class ApplicationInsightsJavaScriptTest
    {
        [Fact]
        public static void SnippetWillBeEmptyWhenInstrumentationKeyIsNotDefined()
        {
            var telemetryConfigurationWithNullKey = new TelemetryConfiguration();
            var snippet = new ApplicationInsightsJavaScript(telemetryConfigurationWithNullKey);
            Assert.Equal(string.Empty, snippet.Write());
        }

        [Fact]
        public static void SnippetWillBeEmptyWhenInstrumentationKeyIsEmpty()
        {
            var telemetryConfigurationWithEmptyKey = new TelemetryConfiguration { InstrumentationKey = string.Empty };
            var snippet = new ApplicationInsightsJavaScript(telemetryConfigurationWithEmptyKey);
            Assert.Equal(string.Empty, snippet.Write());
        }


        [Fact]
        public static void SnippetWillIncludeInstrumentationKeyAsSubstring()
        {
            string unittestkey = "unittestkey";
            var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = unittestkey };
            var snippet = new ApplicationInsightsJavaScript(telemetryConfiguration);
            Assert.Contains("'" + unittestkey + "'", snippet.Write());
        }
    }
}

