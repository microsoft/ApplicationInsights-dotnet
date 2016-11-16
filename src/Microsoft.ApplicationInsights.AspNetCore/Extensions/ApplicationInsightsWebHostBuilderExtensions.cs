using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class ApplicationInsightsWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseApplicationInsights(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureServices(collection =>
            {
                var configBuilder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables();

                collection.AddApplicationInsightsTelemetry(configBuilder.Build());
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
