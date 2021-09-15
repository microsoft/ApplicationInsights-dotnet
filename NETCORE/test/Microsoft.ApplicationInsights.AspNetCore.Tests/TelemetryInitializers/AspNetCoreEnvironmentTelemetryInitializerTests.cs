namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    //using Microsoft.AspNetCore.Hosting.Internal;
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
            var environment = new Moq.Mock<IHostEnvironment>();
            environment.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment.Object);
            var telemetry = new RequestTelemetry();
            telemetry.Context.GlobalProperties.Add("AspNetCoreEnvironment", "Development");
            initializer.Initialize(telemetry);

            Assert.Equal("Development", telemetry.Context.GlobalProperties["AspNetCoreEnvironment"]);
            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty()
        {
            var environment = new Moq.Mock<IHostEnvironment>();
            environment.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment.Object);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }
    }
}
