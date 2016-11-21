namespace Microsoft.AspNetCore.Hosting
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    internal class DefaultApplicationInsightsServiceConfigureOptions : IConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IHostingEnvironment hostingEnvironment;

        public DefaultApplicationInsightsServiceConfigureOptions(IHostingEnvironment hostingEnvironment)
        {
            this.hostingEnvironment = hostingEnvironment;
        }

        public void Configure(ApplicationInsightsServiceOptions options)
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(this.hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables();
            ApplicationInsightsExtensions.AddTelemetryConfiguration(configBuilder.Build(), options);

            if (Debugger.IsAttached)
            {
                options.DeveloperMode = true;
            }
        }
    }
}