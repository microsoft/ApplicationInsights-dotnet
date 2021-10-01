namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    using Moq;


    using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;



    public class AspNetCoreEnvironmentTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHostingEnvironmentIsNull()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment: null);
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingProperty_IHostingEnvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var mockEnvironment = new Mock<IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
            mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment: mockEnvironment.Object);

            var telemetry = new RequestTelemetry();
            telemetry.Context.GlobalProperties.Add("AspNetCoreEnvironment", "Development");
            initializer.Initialize(telemetry);

            Assert.Equal("Development", telemetry.Context.GlobalProperties["AspNetCoreEnvironment"]);
            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var mockEnvironment = new Mock<IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
            mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment: mockEnvironment.Object);
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("Production", telemetry.Properties["AspNetCoreEnvironment"]);
        }
    }
}
