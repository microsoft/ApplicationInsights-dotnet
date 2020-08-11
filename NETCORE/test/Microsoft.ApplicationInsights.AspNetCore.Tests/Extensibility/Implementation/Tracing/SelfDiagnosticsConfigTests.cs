using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.AspNetCore.Tests.TestFramework;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Extensibility.Implementation.Tracing
{
#if !NET46
    public class SelfDiagnosticsConfigTests
    {
        [Fact]
        public void VerifyDefaultConfiguration()
        {
            IServiceCollection services = new ServiceCollection()
                .AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory() })
                .AddApplicationInsightsTelemetry();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Get telemetry client to trigger TelemetryConfig setup.
            var tc = serviceProvider.GetService<TelemetryClient>();

            // Verify that Modules were added to DI.
            var modules = serviceProvider.GetServices<ITelemetryModule>();
            Assert.NotNull(modules);

            var diagnosticsTelemetryModule = modules.OfType<DiagnosticsTelemetryModule>().Single();
            Assert.True(diagnosticsTelemetryModule.IsInitialized);
            Assert.False(diagnosticsTelemetryModule.IsFileLogEnabled);
        }

        [Fact(Skip = "This test works, but causes significant performance issues on the build server.")]
        public void VerifyCanConfigureViaEnvironmentVariable()
        {
            try
            {
                string testLogDirectory = "C:\\Temp";
                this.SetEnvironmentVariable(testLogDirectory);

                IServiceCollection services = new ServiceCollection()
                    .AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory() })
                    .AddApplicationInsightsTelemetry();

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Get telemetry client to trigger TelemetryConfig setup.
                var tc = serviceProvider.GetService<TelemetryClient>();

                // Verify that Modules were added to DI.
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                var diagnosticsTelemetryModule = modules.OfType<DiagnosticsTelemetryModule>().Single();
                Assert.True(diagnosticsTelemetryModule.IsInitialized);
                Assert.True(diagnosticsTelemetryModule.IsFileLogEnabled);
                Assert.Equal(testLogDirectory, diagnosticsTelemetryModule.FileLogDirectory);
            }
            finally
            {
                PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyCanManuallyConfigure(bool enableSelfDiagnosticsFileLogging)
        {
            IServiceCollection services = new ServiceCollection()
                .AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory() })
                .AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
                {
                    EnableSelfDiagnosticsFileLogging = enableSelfDiagnosticsFileLogging
                });

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Get telemetry client to trigger TelemetryConfig setup.
            var tc = serviceProvider.GetService<TelemetryClient>();

            // Verify that Modules were added to DI.
            var modules = serviceProvider.GetServices<ITelemetryModule>();
            Assert.NotNull(modules);

            var diagnosticsTelemetryModule = modules.OfType<DiagnosticsTelemetryModule>().Single();
            Assert.True(diagnosticsTelemetryModule.IsInitialized);
            Assert.Equal(enableSelfDiagnosticsFileLogging, diagnosticsTelemetryModule.IsFileLogEnabled);
        }

        [Fact(Skip = "This test works, but causes significant performance issues on the build server.")]
        public void VerifyEnvironmentVariableOverridesManualConfig()
        {
            try
            {
                string testLogDirectory = "C:\\Temp";
                this.SetEnvironmentVariable(testLogDirectory);

                IServiceCollection services = new ServiceCollection()
                    .AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory() })
                    .AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
                    {
                        EnableSelfDiagnosticsFileLogging = false
                    })
                    .ConfigureTelemetryModule<DiagnosticsTelemetryModule>((module, options) =>
                    {
                        module.FileLogDirectory = "C:\\Temp2";
                    });

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                // Get telemetry client to trigger TelemetryConfig setup.
                var tc = serviceProvider.GetService<TelemetryClient>();

                // Verify that Modules were added to DI.
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);

                var diagnosticsTelemetryModule = modules.OfType<DiagnosticsTelemetryModule>().Single();
                Assert.True(diagnosticsTelemetryModule.IsInitialized);
                Assert.True(diagnosticsTelemetryModule.IsFileLogEnabled);
                Assert.Equal(testLogDirectory, diagnosticsTelemetryModule.FileLogDirectory);
            }
            finally
            {
                PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void VerifyCanConfigureFromJson(bool enableDiagnosticsTelemetryModule, bool enableSelfDiagnosticsFileLogging)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddMockJsonWithFileLoggingConfig(enableDiagnosticsTelemetryModule, enableSelfDiagnosticsFileLogging)
                .Build();

            IServiceCollection services = new ServiceCollection()
                .AddSingleton<IHostingEnvironment>(new HostingEnvironment() { ContentRootPath = Directory.GetCurrentDirectory() })
                .AddSingleton<IConfiguration>(config)
                .AddApplicationInsightsTelemetry();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Get telemetry client to trigger TelemetryConfig setup.
            var tc = serviceProvider.GetService<TelemetryClient>();

            // Verify that Modules were added to DI.
            var modules = serviceProvider.GetServices<ITelemetryModule>();
            Assert.NotNull(modules);

            var diagnosticsTelemetryModule = modules.OfType<DiagnosticsTelemetryModule>().Single();
            Assert.Equal(enableDiagnosticsTelemetryModule, diagnosticsTelemetryModule.IsInitialized);
            Assert.Equal(enableSelfDiagnosticsFileLogging, diagnosticsTelemetryModule.IsFileLogEnabled);
        }

        /// <summary>
        /// Writes a string like "Destination=File;Directory=C:\\Temp;";
        /// </summary>
        /// <param name="logDirectory"></param>
        private void SetEnvironmentVariable(string logDirectory)
        {
            var platform = new StubEnvironmentVariablePlatform();
            platform.SetEnvironmentVariable(DiagnosticsTelemetryModule.SelfDiagnosticsEnvironmentVariable, $"{SelfDiagnosticsProvider.KeyDestination}={SelfDiagnosticsProvider.ValueFile};{SelfDiagnosticsProvider.KeyDirectory}={logDirectory}");
            PlatformSingleton.Current = platform;
        }
    }
#endif
}
