namespace Microsoft.ApplicationInsights.AspNet
{
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNet.Tests;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http.Core;
    using Xunit;

    public class RequestTrackingMiddlewareTest
    {
        private ITelemetry sentTelemetry;

        [Fact]
        public async Task SdkVersionIsPopulatedByMiddleware()
        {
            RequestDelegate nextMiddleware = async httpContext => {
                httpContext.Response.StatusCode = 200;
                await httpContext.Response.Body.WriteAsync(new byte[0], 0, 0);
            };
            var middleware = new RequestTrackingMiddleware(nextMiddleware, MockTelemetryClient());

            await middleware.Invoke(new DefaultHttpContext(), new RequestTelemetry());

            Assert.NotEmpty(sentTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains("aspnetv5", sentTelemetry.Context.GetInternalContext().SdkVersion);
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
