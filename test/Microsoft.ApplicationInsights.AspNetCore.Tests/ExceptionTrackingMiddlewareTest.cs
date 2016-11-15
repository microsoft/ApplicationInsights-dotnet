using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
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
            var middleware = new AspNetCoreHostingListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));

            middleware.OnHostingException(null, null);

            Assert.Equal(ExceptionHandledAt.Platform, ((ExceptionTelemetry)sentTelemetry).HandledAt);
        }

        [Fact]
        public async Task SdkVersionIsPopulatedByMiddleware()
        {
            var middleware = new AspNetCoreHostingListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));

            middleware.OnHostingException(null, null);

            Assert.NotEmpty(sentTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, sentTelemetry.Context.GetInternalContext().SdkVersion);
        }
    }
}
