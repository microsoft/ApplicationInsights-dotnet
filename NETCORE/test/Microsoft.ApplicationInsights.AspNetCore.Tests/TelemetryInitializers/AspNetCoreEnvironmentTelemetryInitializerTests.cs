﻿namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Xunit;

    public class AspNetCoreEnvironmentTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHostingEnvironmentIsNull()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(null);
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingProperty()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(new HostingEnvironment() { EnvironmentName = "Production"});
            var telemetry = new RequestTelemetry();
            telemetry.Context.GlobalProperties.Add("AspNetCoreEnvironment", "Development");
            initializer.Initialize(telemetry);

            Assert.Equal("Development", telemetry.Context.GlobalProperties["AspNetCoreEnvironment"]);
            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(new HostingEnvironment() { EnvironmentName = "Production"});
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }
    }
}
