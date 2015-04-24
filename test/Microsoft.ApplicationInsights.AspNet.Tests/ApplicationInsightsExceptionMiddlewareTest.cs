namespace Microsoft.ApplicationInsights.AspNet
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNet.Tests;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNet.Builder;
    using Xunit;

    public class ApplicationInsightsExceptionMiddlewareTest
    {
        private ITelemetry sentTelemetry;

        [Fact]
        public async Task InvokeTracksExceptionThrownByNextMiddlewareAsHandledByPlatform()
        {
            RequestDelegate nextMiddleware = httpContext => { throw new Exception(); };
            var middleware = new ApplicationInsightsExceptionMiddleware(nextMiddleware, MockTelemetryClient());

            await Assert.ThrowsAnyAsync<Exception>(() => middleware.Invoke(null));

            Assert.Equal(ExceptionHandledAt.Platform, ((ExceptionTelemetry)sentTelemetry).HandledAt);
        }

        private TelemetryClient MockTelemetryClient()
        {
            var telemetryChannel = new FakeTelemetryChannel { OnSend = telemetry => this.sentTelemetry = telemetry };

            var telemetryConfiguration = new TelemetryConfiguration();
            telemetryConfiguration.InstrumentationKey = "REQUIRED";
            telemetryConfiguration.TelemetryChannel = telemetryChannel;

            return new TelemetryClient(telemetryConfiguration);
        }
    }
}
