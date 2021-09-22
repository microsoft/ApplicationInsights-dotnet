namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Xunit;
    using Moq;

    public class AspNetCoreEnvironmentTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHostingEnvironmentIsNull()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var initializer1 = new AspNetCoreEnvironmentTelemetryInitializer(environment: null);
#pragma warning restore CS0618 // Type or member is obsolete
            initializer1.Initialize(new RequestTelemetry());

            var initializer2 = new AspNetCoreEnvironmentTelemetryInitializer(hostEnvironment: null);
            initializer2.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingProperty_IHostingEnrvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var environment = new Mock<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
            environment.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment.Object);
            var telemetry = new RequestTelemetry();
            telemetry.Context.GlobalProperties.Add("AspNetCoreEnvironment", "Development");
            initializer.Initialize(telemetry);

            Assert.Equal("Development", telemetry.Context.GlobalProperties["AspNetCoreEnvironment"]);
            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty_IHostingEnrvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var environmentMock = new Mock<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
            environmentMock.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environmentMock.Object);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingProperty()
        {
            var environment = new Mock<IHostEnvironment>();
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
            var environmentMock = new Mock<IHostEnvironment>();
            environmentMock.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environmentMock.Object);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }
    }
}
