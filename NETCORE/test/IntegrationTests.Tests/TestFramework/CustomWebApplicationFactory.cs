using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace IntegrationTests.Tests.TestFramework
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        internal TelemetryBag sentItems = new TelemetryBag();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(loggingBuilder => loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.None));

                services.AddSingleton<ITelemetryChannel>(new StubChannel()
                {
                    OnSend = (item) => this.sentItems.Add(item)
                });

                var aiOptions = new ApplicationInsightsServiceOptions
                {
                    AddAutoCollectedMetricExtractor = false,
                    EnableAdaptiveSampling = false,
                    InstrumentationKey = "ikey",
                };

                services.AddApplicationInsightsTelemetry(aiOptions);
            });
        }
    }
}
