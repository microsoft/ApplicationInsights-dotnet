using Xunit;

//[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Microsoft.Extensions.DependencyInjection.Test
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.Extensions.Configuration;

#pragma warning disable CS0618 // TelemetryConfiguration.Active is obsolete. We still test with this for backwards compatibility.
    [CollectionDefinition(nameof(ApplicationInsightsExtensionsTests), DisableParallelization = true)]
    public abstract class ApplicationInsightsExtensionsTests
    {
        internal const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";
        internal const string TestConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=http://127.0.0.1";
        internal const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        internal const string ConnectionStringEnvironmentVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        public static ServiceCollection GetServiceCollectionWithContextAccessor()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory()});
            services.AddSingleton<DiagnosticListener>(new DiagnosticListener("TestListener"));
            return services;
        }

        public static ServiceCollection CreateServicesAndAddApplicationinsightsTelemetry(string jsonPath, string channelEndPointAddress, Action<ApplicationInsightsServiceOptions> serviceOptions = null, bool addChannel = true)
        {
            var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
            if (addChannel)
            {
                services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
            }

            IConfigurationRoot config = null;

            if (jsonPath != null)
            {
                var jsonFullPath = Path.Combine(Directory.GetCurrentDirectory(), jsonPath);
                Console.WriteLine("json:" + jsonFullPath);
                Trace.WriteLine("json:" + jsonFullPath);
                try
                {
                    config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(jsonFullPath).Build();
                }
                catch(Exception)
                {
                    throw new Exception("Unable to build with json:" + jsonFullPath);
                }
            }
            else  if (channelEndPointAddress != null)
            {
                config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: channelEndPointAddress).Build();
            }
            else
            {
                config = new ConfigurationBuilder().Build();
            }

            services.AddApplicationInsightsTelemetry(config);
            if (serviceOptions != null)
            {
                services.Configure(serviceOptions);
            }
            return services;
        }
    }
#pragma warning restore CS0618 // TelemetryConfiguration.Active is obsolete. We still test with this for backwards compatibility.
}