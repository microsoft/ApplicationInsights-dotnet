using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
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
            platform.SetEnvironmentVariable(DiagnosticsTelemetryModule.SelfDiagnosticsEnvironmentVariable, $"{SelfDiagnosticsProvider.KeyDestination}={SelfDiagnosticsProvider.ValueDestinationFile};{SelfDiagnosticsProvider.KeyFilePath}={logDirectory}");
            PlatformSingleton.Current = platform;
        }
    }

    internal class FakeDebugOutput : IDebugOutput
    {
        public void WriteLine(string message)
        {
        }

        public bool IsLogging() => false;

        public bool IsAttached() => false;
    }

    internal class StubEnvironmentVariablePlatform : IPlatform
    {
        private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();

        public void SetEnvironmentVariable(string name, string value) => this.environmentVariables.Add(name, value);

        public bool TryGetEnvironmentVariable(string name, out string value) => this.environmentVariables.TryGetValue(name, out value);

        public string ReadConfigurationXml() => null;

        public IDebugOutput GetDebugOutput() => new FakeDebugOutput();

        public string GetMachineName() => nameof(SelfDiagnosticsConfigTests);
    }

    static class IConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddMockJsonWithFileLoggingConfig(this IConfigurationBuilder builder, bool enableDiagnosticsTelemetryModule, bool enableSelfDiagnosticsFileLogging)
        {
            var appSettings = $@"{{
                ""ApplicationInsights"": {{
                    ""{nameof(ApplicationInsightsServiceOptions.InstrumentationKey)}"": ""testIkey"",
                    ""{nameof(ApplicationInsightsServiceOptions.EnableDiagnosticsTelemetryModule)}"": ""{enableDiagnosticsTelemetryModule}"",
                    ""{nameof(ApplicationInsightsServiceOptions.EnableSelfDiagnosticsFileLogging)}"": ""{enableSelfDiagnosticsFileLogging}"",
                    }}
                }}";

            return builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appSettings)));
        }
    }
#endif
}
