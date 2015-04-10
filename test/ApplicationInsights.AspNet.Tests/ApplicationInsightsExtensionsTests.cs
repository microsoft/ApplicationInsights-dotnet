namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Hosting;
    using Microsoft.Framework.ConfigurationModel;
    using Microsoft.Framework.DependencyInjection;
    using Microsoft.Framework.DependencyInjection.Fallback;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class ApplicationInsightsExtensionsTests
    {
        [Fact]
        public void AddTelemetryWillNotThrowWithoutInstrumentationKey()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);

                //Empty configuration that doesn't have instrumentation key
                IConfiguration config = new Configuration();

                serviceCollection.AddApplicationInsightsTelemetry(config);
            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void AddTelemetryWillUseInstrumentationKeyFromConfig()
        {
            try
            {
                var serviceCollection = HostingServices.Create(null);
                IConfiguration config = new Configuration().AddJsonFile("content\\config.json");

                serviceCollection.AddApplicationInsightsTelemetry(config);

                Assert.Equal("11111111-2222-3333-4444-555555555555", TelemetryConfiguration.Active.InstrumentationKey);
            }
            finally
            {
                CleanActiveConfiguration();
            }
        }

        [Fact]
        public void AddTelemetryRegistersTelemetryClientServiceWithScopedLifecycleToPreventSharingOfRequestSpecificProperties()
        {
            var services = new ServiceCollection();

            services.AddApplicationInsightsTelemetry(new Configuration());

            var service = services.Single(s => s.ServiceType == typeof(TelemetryClient));
            Assert.Equal(LifecycleKind.Scoped, service.Lifecycle);
        }

        [Fact]
        public void JSSnippetWillNotThrowWithoutInstrumentationKey()
        {
            var helper = new HtmlHelperMock();
            helper.ApplicationInsightsJavaScriptSnippet(null);
            helper.ApplicationInsightsJavaScriptSnippet("");
        }

        [Fact]
        public void JSSnippetUsesInstrumentationKey()
        {
            var key = "1236543";
            HtmlHelperMock helper = new HtmlHelperMock();
            var result = helper.ApplicationInsightsJavaScriptSnippet(key);
            using (StringWriter sw = new StringWriter())
            {
                result.WriteTo(sw);
                Assert.Contains(key, sw.ToString());
            }
        }

        private void CleanActiveConfiguration()
        {
            TelemetryConfiguration.Active.InstrumentationKey = "";
            TelemetryConfiguration.Active.ContextInitializers.Clear();
            TelemetryConfiguration.Active.TelemetryModules.Clear();
            TelemetryConfiguration.Active.TelemetryInitializers.Clear();
        }
    }
}