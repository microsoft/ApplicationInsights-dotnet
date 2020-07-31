using System;

using System.Collections.Generic;
using System.Diagnostics;
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
    public class SelfDiagnosticsConfigTests : IDisposable
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

        [Fact]
        public void VerifyCanConfigureViaEnvironmentVariable()
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

        [Fact]
        public void VerifyEnvironmentVariableOverridesManualConfig()
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

        public void Dispose()
        {
            PlatformSingleton.Current = null; // Force reinitialization in future tests so that new environment variables will be loaded.
        }
    }


    internal class StubDebugOutput : IDebugOutput
    {
        public Action<string> OnWriteLine = message => { };

        public Func<bool> OnIsAttached = () => System.Diagnostics.Debugger.IsAttached;

        public void WriteLine(string message)
        {
            this.OnWriteLine(message);
        }

        public bool IsLogging()
        {
            return true;
        }

        public bool IsAttached()
        {
            return this.OnIsAttached();
        }
    }

    internal class StubPlatform : IPlatform
    {
        public Func<IDebugOutput> OnGetDebugOutput = () => new StubDebugOutput();
        public Func<string> OnReadConfigurationXml = () => null;
        public Func<string> OnGetMachineName = () => null;

        public string ReadConfigurationXml()
        {
            return this.OnReadConfigurationXml();
        }

        public IDebugOutput GetDebugOutput()
        {
            return this.OnGetDebugOutput();
        }

        public virtual bool TryGetEnvironmentVariable(string name, out string value)
        {
            value = string.Empty;

            try
            {
                value = Environment.GetEnvironmentVariable(name);
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception e)
            {
                CoreEventSource.Log.FailedToLoadEnvironmentVariables(e.ToString());
            }

            return false;
        }

        public string GetMachineName()
        {
            return this.OnGetMachineName();
        }
    }

    internal class StubEnvironmentVariablePlatform : StubPlatform
    {
        private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();

        public void SetEnvironmentVariable(string name, string value) => this.environmentVariables.Add(name, value);

        public override bool TryGetEnvironmentVariable(string name, out string value) => this.environmentVariables.TryGetValue(name, out value);

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
