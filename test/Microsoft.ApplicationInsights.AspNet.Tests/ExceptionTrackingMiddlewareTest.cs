namespace Microsoft.ApplicationInsights.AspNet
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNet.Builder;
    using Xunit;

    public class ExceptionTrackingMiddlewareTest
    {
        private ITelemetry sentTelemetry;

        [Fact]
        public async Task InvokeTracksExceptionThrownByNextMiddlewareAsHandledByPlatform()
        {
            RequestDelegate nextMiddleware = httpContext => { throw new Exception(); };
            var middleware = new ExceptionTrackingMiddleware(nextMiddleware, CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));

            await Assert.ThrowsAnyAsync<Exception>(() => middleware.Invoke(null));

            Assert.Equal(ExceptionHandledAt.Platform, ((ExceptionTelemetry)sentTelemetry).HandledAt);
        }

        [Fact]
        public async Task SdkVersionIsPopulatedByMiddleware()
        {
            RequestDelegate nextMiddleware = httpContext => { throw new Exception(); };
            var middleware = new ExceptionTrackingMiddleware(nextMiddleware, CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));

            await Assert.ThrowsAnyAsync<Exception>(() => middleware.Invoke(null));

            Assert.NotEmpty(sentTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains("aspnet5", sentTelemetry.Context.GetInternalContext().SdkVersion);
        }
    }
}
