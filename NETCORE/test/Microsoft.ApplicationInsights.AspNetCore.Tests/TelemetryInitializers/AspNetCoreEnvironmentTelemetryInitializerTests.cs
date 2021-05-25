namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
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
#pragma warning disable CS0618 // Type or member is obsolete
            telemetry.Context.Properties.Add("AspNetCoreEnvironment", "Development");
#pragma warning restore CS0618 // Type or member is obsolete
            initializer.Initialize(telemetry);

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal("Development", telemetry.Context.Properties["AspNetCoreEnvironment"]);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(new HostingEnvironment() { EnvironmentName = "Production"});
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal("Production", telemetry.Context.Properties["AspNetCoreEnvironment"]);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
