namespace Microsoft.ApplicationInsights.AspNet
{
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
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
            var middleware = new RequestTrackingMiddleware(nextMiddleware, CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));

            await middleware.Invoke(new DefaultHttpContext(), new RequestTelemetry());

            Assert.NotEmpty(sentTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains("aspnet5", sentTelemetry.Context.GetInternalContext().SdkVersion);
        }
    }
}
