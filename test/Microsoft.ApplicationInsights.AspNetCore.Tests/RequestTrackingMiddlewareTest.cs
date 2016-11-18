using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    public class RequestTrackingMiddlewareTest
    {
        private const string HttpRequestScheme = "http";
        private readonly HostString httpRequestHost = new HostString("testHost");
        private readonly PathString httpRequestPath = new PathString("/path/path");
        private readonly QueryString httpRequestQueryString = new QueryString("?query=1");

        private ITelemetry sentTelemetry;

        private readonly HostingDiagnosticListener middleware;

        public RequestTrackingMiddlewareTest()
        {
            this.middleware = new HostingDiagnosticListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));
        }

        [Fact]
        public void TestSdkVersionIsPopulatedByMiddleware()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;

            middleware.OnBeginRequest(context, 0);
            middleware.OnEndRequest(context, 0);

            Assert.NotEmpty(this.sentTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, this.sentTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void TestRequestUriIsPopulatedByMiddleware()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;
            context.Request.Path = this.httpRequestPath;
            context.Request.QueryString = this.httpRequestQueryString;

            middleware.OnBeginRequest(context, 0);
            middleware.OnEndRequest(context, 0);

            var telemetry = (RequestTelemetry)sentTelemetry;
            Assert.NotNull(telemetry.Url);

            Assert.Equal(
                new Uri(string.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}{3}", HttpRequestScheme, httpRequestHost.Value, httpRequestPath.Value, httpRequestQueryString.Value)),
                telemetry.Url);
        }

        [Fact]
        public void RequestWillBeMarkedAsFailedForRunawayException()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;

            middleware.OnBeginRequest(context, 0);
            middleware.OnDiagnosticsUnhandledException(context, null);
            middleware.OnEndRequest(context, 0);

            Assert.False(((RequestTelemetry)this.sentTelemetry).Success);
        }

        [Fact]
        public void OnEndRequestSetsRequestNameToMethodAndPathForPostRequest()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Method = "POST";
            context.Request.Host = this.httpRequestHost;
            context.Request.Path = "/Test";

            middleware.OnBeginRequest(context, 0);
            middleware.OnEndRequest(context, 0);

            var telemetry = (RequestTelemetry)sentTelemetry;

            Assert.Equal("POST /Test", telemetry.Name);
        }

        [Fact]
        public void OnEndRequestSetsRequestNameToMethodAndPath()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Method = "GET";
            context.Request.Host = this.httpRequestHost;
            context.Request.Path = "/Test";

            middleware.OnBeginRequest(context, 0);
            middleware.OnEndRequest(context, 0);

            var telemetry = (RequestTelemetry)sentTelemetry;

            Assert.Equal("GET /Test", telemetry.Name);
        }
    }
}
