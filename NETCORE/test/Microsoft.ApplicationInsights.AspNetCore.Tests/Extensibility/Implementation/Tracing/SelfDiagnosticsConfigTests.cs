using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
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
        // TODO:
        // Test manually enabling logging
        // test environment variable enable logging
        // test environment variable overriding manual config


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
