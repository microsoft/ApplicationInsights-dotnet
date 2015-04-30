namespace Microsoft.Framework.DependencyInjection
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Xunit;
    using Microsoft.ApplicationInsights.AspNet.JavaScript;

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

