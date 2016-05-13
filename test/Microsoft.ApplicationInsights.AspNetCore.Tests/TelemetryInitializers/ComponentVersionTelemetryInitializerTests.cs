namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    public class ComponentVersionTelemetryInitializerTests
    {
        private const string VersionKey = "version";

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var initializer = new ComponentVersionTelemetryInitializer(this.BuildConfigurationWithVersion());
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var initializer = new ComponentVersionTelemetryInitializer(this.BuildConfigurationWithVersion());
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeAssignsVersionToTelemetry()
        {
            var initializer = new ComponentVersionTelemetryInitializer(this.BuildConfigurationWithVersion());
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);
            Assert.NotNull(telemetry.Context.Component.Version);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingVersion()
        {
            
            var initializer = new ComponentVersionTelemetryInitializer(this.BuildConfigurationWithVersion());
            var telemetry = new RequestTelemetry();
            telemetry.Context.Component.Version = "TestVersion";
            initializer.Initialize(telemetry);

            Assert.Equal("TestVersion", telemetry.Context.Component.Version);
        }

        [Fact]
        public void InitializeDoesNotThrowIfVersionDoesNotExist()
        {
            var config = new ConfigurationBuilder()
                .Build();
            var initializer = new ComponentVersionTelemetryInitializer(config);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);
        }

        private IConfigurationRoot BuildConfigurationWithVersion()
        {
            Environment.SetEnvironmentVariable(VersionKey, "1.0.0");
            return new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
