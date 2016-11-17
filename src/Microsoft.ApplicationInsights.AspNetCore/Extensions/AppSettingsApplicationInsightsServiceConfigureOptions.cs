using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting
{
    internal class AppSettingsApplicationInsightsServiceConfigureOptions: IConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public AppSettingsApplicationInsightsServiceConfigureOptions(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void Configure(ApplicationInsightsServiceOptions options)
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(_hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables();
            ApplicationInsightsExtensions.AddTelemetryConfiguration(configBuilder.Build(), options);
        }
    }
}