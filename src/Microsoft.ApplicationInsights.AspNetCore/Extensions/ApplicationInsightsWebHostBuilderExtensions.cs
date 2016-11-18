namespace Microsoft.AspNetCore.Hosting
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    public static class ApplicationInsightsWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseApplicationInsights(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureServices(collection =>
            {
                collection.AddApplicationInsightsTelemetry((Action<ApplicationInsightsServiceOptions>)null);
                collection.AddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>, DefaultApplicationInsightsServiceConfigureOptions>();
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
