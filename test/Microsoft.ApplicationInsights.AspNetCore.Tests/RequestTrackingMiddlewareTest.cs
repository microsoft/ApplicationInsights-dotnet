namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Xunit;
    using Microsoft.AspNetCore.Http.Internal;

    public class RequestTrackingMiddlewareTest
    {
        private const string HttpRequestScheme = "http";
        private readonly HostString httpRequestHost = new HostString("testHost");
        private readonly PathString httpRequestPath = new PathString("/path/path");
        private readonly QueryString httpRequestQueryString = new QueryString("?query=1");

        private const string ExpectedSdkVersion = "aspnetCore";

        private ITelemetry sentTelemetry;

        private readonly RequestDelegate nextMiddleware = async httpContext => {
            httpContext.Response.StatusCode = 200;
            await httpContext.Response.Body.WriteAsync(new byte[0], 0, 0);
        };

        private readonly RequestTrackingMiddleware middleware;

        public RequestTrackingMiddlewareTest()
        {
            this.middleware = new RequestTrackingMiddleware(
                this.nextMiddleware,
                CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));
        }

        [Fact]
        public async Task TestSdkVersionIsPopulatedByMiddleware()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;

            await middleware.Invoke(context, new RequestTelemetry());

            Assert.NotEmpty(this.sentTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(ExpectedSdkVersion, this.sentTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public async Task TestRequestUriIsPopulatedByMiddleware()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;
            context.Request.Path = this.httpRequestPath;
            context.Request.QueryString = this.httpRequestQueryString;

            var telemetry = new RequestTelemetry();
            await middleware.Invoke(context, telemetry);

            Assert.NotNull(telemetry.Url);

            Assert.Equal(
                new Uri(string.Format("{0}://{1}{2}{3}", HttpRequestScheme, httpRequestHost.Value, httpRequestPath.Value, httpRequestQueryString.Value)), 
                telemetry.Url);
        }

        [Fact]
        public async Task RequestWillBeMarkedAsFailedForRunawayException()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;

            var requestMiddleware = new RequestTrackingMiddleware(
                httpContext => { throw new InvalidOperationException(); },
                CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));

            await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => { await requestMiddleware.Invoke(context, new RequestTelemetry()); } );

            Assert.False(((RequestTelemetry)this.sentTelemetry).Success);
        }
    }
}
