namespace Microsoft.Framework.DependencyInjection
{
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.Extensibility;
    using Xunit;

    public static class ApplicationInsightsJavaScriptTest
    {
        [Fact]
        public static void SnippetWillBeEmptyWhenInstrumentationKeyIsNotDefined()
        {
            var telemetryConfigurationWithNullKey = new TelemetryConfiguration();
            var snippet = new JavaScriptSnippet(telemetryConfigurationWithNullKey);
            Assert.Equal(string.Empty, snippet.FullScript.ToString());
        }

        [Fact]
        public static void SnippetWillBeEmptyWhenInstrumentationKeyIsEmpty()
        {
            var telemetryConfigurationWithEmptyKey = new TelemetryConfiguration { InstrumentationKey = string.Empty };
            var snippet = new JavaScriptSnippet(telemetryConfigurationWithEmptyKey);
            Assert.Equal(string.Empty, snippet.FullScript.ToString());
        }


        [Fact]
        public static void SnippetWillIncludeInstrumentationKeyAsSubstring()
        {
            string unittestkey = "unittestkey";
            var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = unittestkey };
            var snippet = new JavaScriptSnippet(telemetryConfiguration);
            Assert.Contains("'" + unittestkey + "'", snippet.FullScript.ToString());
        }
    }
}

