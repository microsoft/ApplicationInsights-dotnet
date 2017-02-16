using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Xunit;
    using System.Diagnostics;

    public class RequestTrackingMiddlewareTest
    {
        private const string HttpRequestScheme = "http";
        private readonly HostString httpRequestHost = new HostString("testHost");
        private readonly PathString httpRequestPath = new PathString("/path/path");
        private readonly QueryString httpRequestQueryString = new QueryString("?query=1");

        private List<ITelemetry> sentTelemetry = new List<ITelemetry>();

        private readonly HostingDiagnosticListener middleware;

        public RequestTrackingMiddlewareTest()
        {
            this.middleware = new HostingDiagnosticListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry.Add(telemetry)));
        }

        [Fact]
        public void TestSdkVersionIsPopulatedByMiddleware()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;

            middleware.OnBeginRequest(context, 0);
            middleware.OnEndRequest(context, 0);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.NotEmpty(this.sentTelemetry[0].Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, this.sentTelemetry[0].Context.GetInternalContext().SdkVersion);
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

            Assert.Equal(1, sentTelemetry.Count);
            var telemetry = (RequestTelemetry)sentTelemetry[0];
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

            Assert.Equal(2, sentTelemetry.Count);
            Assert.True(this.sentTelemetry[0] is ExceptionTelemetry);
            Assert.False(((RequestTelemetry)this.sentTelemetry[1]).Success);
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

            Assert.Equal(1, sentTelemetry.Count);
            var telemetry = (RequestTelemetry)sentTelemetry[0];

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

            Assert.Equal(1, sentTelemetry.Count);
            var telemetry = (RequestTelemetry)sentTelemetry[0];

            Assert.Equal("GET /Test", telemetry.Name);
        }

        [Fact]
        public void SimultaneousRequestsGetDifferentIds()
        {
            var context1 = new DefaultHttpContext();
            context1.Request.Scheme = HttpRequestScheme;
            context1.Request.Host = this.httpRequestHost;
            context1.Request.Method = "GET";
            context1.Request.Path = "/Test?id=1";

            var context2 = new DefaultHttpContext();
            context2.Request.Scheme = HttpRequestScheme;
            context2.Request.Host = this.httpRequestHost;
            context2.Request.Method = "GET";
            context2.Request.Path = "/Test?id=2";

            middleware.OnBeginRequest(context1, 0);
            middleware.OnBeginRequest(context2, 0);
            middleware.OnEndRequest(context1, 0);
            middleware.OnEndRequest(context2, 0);

            Assert.Equal(2, sentTelemetry.Count);
            var id1 = ((RequestTelemetry)sentTelemetry[0]).Id;
            var id2 = ((RequestTelemetry)sentTelemetry[1]).Id;
            Assert.Equal(context1.TraceIdentifier, id1);
            Assert.Equal(context2.TraceIdentifier, id2);
            Assert.NotEqual(id1, id2);
        }

        [Fact(Skip = "Disabled until timestamp overloads are supported in OperationTelemetryExtensions.Start/Stop. https://github.com/Microsoft/ApplicationInsights-dotnet/pull/424")]
        public void SimultaneousRequestsGetCorrectDurations()
        {
            var context1 = new DefaultHttpContext();
            context1.Request.Scheme = HttpRequestScheme;
            context1.Request.Host = this.httpRequestHost;
            context1.Request.Method = "GET";
            context1.Request.Path = "/Test?id=1";

            var context2 = new DefaultHttpContext();
            context2.Request.Scheme = HttpRequestScheme;
            context2.Request.Host = this.httpRequestHost;
            context2.Request.Method = "GET";
            context2.Request.Path = "/Test?id=2";

            long startTime = Stopwatch.GetTimestamp();
            long simulatedSeconds = Stopwatch.Frequency;

            middleware.OnBeginRequest(context1, timestamp: startTime);
            middleware.OnBeginRequest(context2, timestamp: startTime + simulatedSeconds);
            middleware.OnEndRequest(context1, timestamp: startTime + simulatedSeconds * 5);
            middleware.OnEndRequest(context2, timestamp: startTime + simulatedSeconds * 10);

            Assert.Equal(2, sentTelemetry.Count);
            Assert.Equal(TimeSpan.FromSeconds(5), ((RequestTelemetry)sentTelemetry[0]).Duration);
            Assert.Equal(TimeSpan.FromSeconds(9), ((RequestTelemetry)sentTelemetry[1]).Duration);
        }

        [Fact(Skip = "Disabled until precise durations are supported in OperationTelemetryExtensions.Start/Stop. https://github.com/Microsoft/ApplicationInsights-dotnet/pull/426")]
        public void OnEndRequestSetsPreciseDurations()
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = HttpRequestScheme;
            context.Request.Host = this.httpRequestHost;
            context.Request.Method = "GET";
            context.Request.Path = "/Test?id=1";

            long startTime = Stopwatch.GetTimestamp();
            middleware.OnBeginRequest(context, timestamp: startTime);

            var expectedDuration = TimeSpan.Parse("00:00:01.2345670");
            double durationInStopwatchTicks = Stopwatch.Frequency * expectedDuration.TotalSeconds;

            middleware.OnEndRequest(context, timestamp: startTime + (long)durationInStopwatchTicks);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.Equal(expectedDuration, ((RequestTelemetry)sentTelemetry[0]).Duration);
        }
    }
}
