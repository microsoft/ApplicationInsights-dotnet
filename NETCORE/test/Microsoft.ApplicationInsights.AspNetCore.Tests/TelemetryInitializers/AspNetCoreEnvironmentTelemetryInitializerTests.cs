namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class AspNetCoreEnvironmentTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHostingEnvironmentIsNull_IHostingEnvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var initializer1 = new AspNetCoreEnvironmentTelemetryInitializer(environment: null);
#pragma warning restore CS0618 // Type or member is obsolete
            initializer1.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingProperty_IHostingEnvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment: EnvironmentHelper.GetIHostingEnvironment());
#pragma warning restore CS0618 // Type or member is obsolete
            var telemetry = new RequestTelemetry();
            telemetry.Context.GlobalProperties.Add("AspNetCoreEnvironment", "Development");
            initializer.Initialize(telemetry);

            Assert.Equal("Development", telemetry.Context.GlobalProperties["AspNetCoreEnvironment"]);
            Assert.Equal("UnitTest", telemetry.Properties["AspNetCoreEnvironment"]);
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty_IHostingEnvironment()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(environment: EnvironmentHelper.GetIHostingEnvironment());
#pragma warning restore CS0618 // Type or member is obsolete
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("UnitTest", telemetry.Properties["AspNetCoreEnvironment"]);
        }

#if NETCOREAPP
        [Fact]
        public void InitializeDoesNotThrowIfHostingEnvironmentIsNull_IHostEnvironment()
        {
            var initializer2 = new AspNetCoreEnvironmentTelemetryInitializer(hostEnvironment: null);
            initializer2.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingProperty_IHostEnvironment()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(hostEnvironment: EnvironmentHelper.GetIHostEnvironment());
            var telemetry = new RequestTelemetry();
            telemetry.Context.GlobalProperties.Add("AspNetCoreEnvironment", "Development");
            initializer.Initialize(telemetry);

            Assert.Equal("Development", telemetry.Context.GlobalProperties["AspNetCoreEnvironment"]);
            Assert.Equal("UnitTest", telemetry.Properties["AspNetCoreEnvironment"]);
        }

        [Fact]
        public void InitializeSetsCurrentEnvironmentNameToProperty_IHostEnvironment()
        {
            var initializer = new AspNetCoreEnvironmentTelemetryInitializer(hostEnvironment: EnvironmentHelper.GetIHostEnvironment());
            var telemetry = new RequestTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("UnitTest", telemetry.Properties["AspNetCoreEnvironment"]);
        }
#endif
    }
}
