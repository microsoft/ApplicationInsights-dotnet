namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Xunit;

    public class ComponentVersionTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var initializer = new ComponentVersionTelemetryInitializer();
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };
            var initializer = new ComponentVersionTelemetryInitializer();
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingVersion()
        {
            var initializer = new ComponentVersionTelemetryInitializer();

            var telemetry = new RequestTelemetry();
            telemetry.Context.Component.Version = "TestVersion";
            initializer.Initialize(telemetry);

            Assert.Equal("TestVersion", telemetry.Context.Component.Version);
        }
    }
}
