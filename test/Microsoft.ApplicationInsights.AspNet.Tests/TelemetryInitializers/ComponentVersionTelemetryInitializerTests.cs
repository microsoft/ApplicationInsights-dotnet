namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http.Internal;
    using Xunit;

    public class ComponentVersionTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };
            var initializer = new ComponentVersionTelemetryInitializer(ac);
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };
            var initializer = new ComponentVersionTelemetryInitializer(ac);
            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideExistingVersion()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };
            var initializer = new ComponentVersionTelemetryInitializer(ac);

            var telemetry = new RequestTelemetry();
            telemetry.Context.Component.Version = "TestVersion";
            initializer.Initialize(telemetry);

            Assert.Equal("TestVersion", telemetry.Context.Component.Version);
        }
    }
}
