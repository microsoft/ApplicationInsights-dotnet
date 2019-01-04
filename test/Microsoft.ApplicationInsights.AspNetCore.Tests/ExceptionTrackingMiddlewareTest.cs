using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Xunit;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{

    public class ExceptionTrackingMiddlewareTest
    {
        private ITelemetry sentTelemetry;

        [Fact]
        public void InvokeTracksExceptionThrownByNextMiddlewareAsHandledByPlatform()
        {
            using (var hostingListener = new HostingDiagnosticListener(
                CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry),
                CommonMocks.GetMockApplicationIdProvider(),
                injectResponseHeaders: true,
                trackExceptions: true,
                enableW3CHeaders: false,
                enableNewDiagnosticEvents: true))
            {
                hostingListener.OnSubscribe();
                hostingListener.OnHostingException(null, null);

                Assert.NotNull(sentTelemetry);
                Assert.IsType<ExceptionTelemetry>(sentTelemetry);
                Assert.Equal(ExceptionHandledAt.Platform, ((ExceptionTelemetry) sentTelemetry).HandledAt);
            }
        }

        [Fact]
        public void SdkVersionIsPopulatedByMiddleware()
        {
            using (var hostingListener = new HostingDiagnosticListener(
                CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry),
                CommonMocks.GetMockApplicationIdProvider(),
                injectResponseHeaders: true,
                trackExceptions: true,
                enableW3CHeaders: false,
                enableNewDiagnosticEvents: true))
            {
                hostingListener.OnSubscribe();
                hostingListener.OnHostingException(null, null);

                Assert.NotEmpty(sentTelemetry.Context.GetInternalContext().SdkVersion);
                Assert.Contains(SdkVersionTestUtils.VersionPrefix,
                    sentTelemetry.Context.GetInternalContext().SdkVersion);
            }
        }
    }
}
