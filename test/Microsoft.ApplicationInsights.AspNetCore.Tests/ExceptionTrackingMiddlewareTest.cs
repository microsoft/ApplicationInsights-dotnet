namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Xunit;
    using System.Threading.Tasks;

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
            Assert.Contains("aspnetCore", sentTelemetry.Context.GetInternalContext().SdkVersion);
        }
    }
}
