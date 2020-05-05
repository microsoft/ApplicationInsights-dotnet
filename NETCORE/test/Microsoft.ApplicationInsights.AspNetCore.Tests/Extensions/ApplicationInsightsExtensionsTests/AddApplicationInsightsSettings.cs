using Xunit;

//[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Microsoft.Extensions.DependencyInjection.Test
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.AspNetCore.Logging;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
#if NETCOREAPP
    using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
#endif
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.W3C;
    using Microsoft.ApplicationInsights.WindowsServer;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    public class AddApplicationInsightsSettings : ApplicationInsightsExtensionsTestsBaseClass
    {
        [Fact]
        public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromSettings()
        {
            var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
            services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
            var config = new ConfigurationBuilder().AddApplicationInsightsSettings(instrumentationKey: TestInstrumentationKey).Build();
            services.AddApplicationInsightsTelemetry(config);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
            Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
        }

        [Fact]
        public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromSettings()
        {
            var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
            services.AddSingleton<ITelemetryChannel>(new InMemoryChannel());
            var config = new ConfigurationBuilder().AddApplicationInsightsSettings(developerMode: true).Build();
            services.AddApplicationInsightsTelemetry(config);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
            Assert.True(telemetryConfiguration.TelemetryChannel.DeveloperMode);
        }

        [Fact]
        public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromSettings()
        {
            var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/");
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var telemetryConfiguration = serviceProvider.GetTelemetryConfiguration();
            Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
        }

        /// <summary>
        /// Sanity check to validate that node name and roleinstance are populated
        /// </summary>
        [Fact]
        public static void SanityCheckRoleInstance()
        {
            // ARRANGE
            string expected = Environment.MachineName;
            var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
            services.AddApplicationInsightsTelemetry();
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Request TC from DI which would be made with the default TelemetryConfiguration which should 
            // contain the telemetry initializer capable of populate node name and role instance name.
            var tc = serviceProvider.GetRequiredService<TelemetryClient>();
            var mockItem = new EventTelemetry();

            // ACT
            // This is expected to run all TI and populate the node name and role instance.
            tc.Initialize(mockItem);

            // VERIFY                
            Assert.Contains(expected, mockItem.Context.Cloud.RoleInstance, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
