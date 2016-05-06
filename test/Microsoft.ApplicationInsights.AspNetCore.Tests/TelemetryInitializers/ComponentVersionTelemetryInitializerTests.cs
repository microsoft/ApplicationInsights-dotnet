namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Extensions.Configuration;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using System.IO;
    using Xunit;

    public class ComponentVersionTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("project.json")
                .Build();
            var initializer = new ComponentVersionTelemetryInitializer(config);
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("project.json")
                .Build();
            var initializer = new ComponentVersionTelemetryInitializer(config);
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeAssignsVersionToTelemetry()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("project.json")
                .Build();
            var initializer = new ComponentVersionTelemetryInitializer(config);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);
            Assert.NotNull(telemetry.Context.Component.Version);
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingVersion()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("project.json")
                .Build();
            var initializer = new ComponentVersionTelemetryInitializer(config);

            var telemetry = new RequestTelemetry();
            telemetry.Context.Component.Version = "TestVersion";
            initializer.Initialize(telemetry);

            Assert.Equal("TestVersion", telemetry.Context.Component.Version);
        }

        [Fact]
        public void InitializeDoesNotThrowIfVersionDoesNotExist()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();
            var initializer = new ComponentVersionTelemetryInitializer(config);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);
        }
    }
}
