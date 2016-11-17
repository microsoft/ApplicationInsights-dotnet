using System;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting
{
    public static class ApplicationInsightsWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseApplicationInsights(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureServices(collection =>
            {
                collection.AddApplicationInsightsTelemetry((Action<ApplicationInsightsServiceOptions>)null);
                collection.AddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>, AppSettingsApplicationInsightsServiceConfigureOptions>();
            });
            return webHostBuilder;
        }

        public static IWebHostBuilder UseApplicationInsights(this IWebHostBuilder webHostBuilder, string instrumentationKey)
        {
            webHostBuilder.ConfigureServices(collection => collection.AddApplicationInsightsTelemetry(instrumentationKey));
            return webHostBuilder;
        }
    }
}
