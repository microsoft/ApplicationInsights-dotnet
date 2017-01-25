namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Xunit;

    public class DependencyCollectorDiagnosticListenerTest
    {
        private readonly DependencyCollectorDiagnosticListener listener;
        private ITelemetry sentTelemetry;

        public DependencyCollectorDiagnosticListenerTest()
        {
            this.listener = new DependencyCollectorDiagnosticListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));
        }

        [Fact]
        public void OnRequestSentShouldNotSendTelemetry()
        {
            listener.OnRequestSent(new HttpRequestMessage(HttpMethod.Get, "http://www.microsoft.com/test/path.html"), new Guid(), 0);
            Assert.Null(this.sentTelemetry); // Telemetry shouldn't be logged until OnResponseReceived().
        }

        [Fact]
        public void OnResponseReceivedWithAssociatedRequest()
        {
            Guid loggingId = new Guid();
            Uri requestUri = new Uri("http://www.microsoft.com/test/path.html");
            listener.OnRequestSent(new HttpRequestMessage(HttpMethod.Get, requestUri.ToString()), loggingId, 0);
            listener.OnResponseReceived(new HttpResponseMessage(HttpStatusCode.OK), loggingId, 1);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<DependencyTelemetry>(this.sentTelemetry);

            DependencyTelemetry dependencyTelemetry = this.sentTelemetry as DependencyTelemetry;
            Assert.Equal("GET " + uri.AbsolutePath, dependencyTelemetry.Name);
            Assert.Equal(uri.Host, dependencyTelemetry.Target);
            Assert.Equal(uri.OriginalString, dependencyTelemetry.Data);
            Assert.Equal(type.ToString(), dependencyTelemetry.Type);
            Assert.Equal(success, dependencyTelemetry.Success);
            Assert.Equal(resultCode, dependencyTelemetry.ResultCode);

            int TimeAccuracyMilliseconds = 150; // this may be big number when under debugger

            double valueMinRelaxed = expectedValue - TimeAccuracyMilliseconds;
            Assert.True(
                dependencyTelemetry.Duration >= TimeSpan.FromMilliseconds(valueMinRelaxed),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should be equal or more than the time duration between start and end", dependencyTelemetry.Duration));

            double valueMax = expectedValue + TimeAccuracyMilliseconds;
            Assert.True(
                dependencyTelemetry.Duration <= TimeSpan.FromMilliseconds(valueMax),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should not be significantly bigger than the time duration between start and end", dependencyTelemetry.Duration));

            string expectedVersion = GetExpectedSdkVersion(typeof(DependencyCollectorDiagnosticListenerTest), prefix: "rddp:");
            Assert.Equal(expectedVersion, dependencyTelemetry.Context.GetInternalContext().SdkVersion);
            ValidateDependencyTelemetry(this.sentTelemetry as DependencyTelemetry, new Uri("http://www.microsoft.com/test/path.html"), "Http", true, 0, "MOCKRESULT");
        }

        [Fact]
        public void OnResponseReceivedWithNoAssociatedRequest()
        {
            listener.OnResponseReceived(new HttpResponseMessage(HttpStatusCode.OK), new Guid(), 1);
            Assert.Null(this.sentTelemetry); // If there wasn't an associated request sent, then OnResponseReceived() shouldn't do anything.
        }

        private static string GetExpectedSdkVersion(Type assemblyType, string prefix)
        {
            string versionString = Assembly.GetEntryAssembly().GetCustomAttributes()
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
            string[] versionParts = new Version(versionString).ToString().Split('.');

            return prefix + string.Join(".", versionParts[0], versionParts[1], versionParts[2]) + "-" + versionParts[3];
        }

        //[Fact]
        //public void TestRequestUriIsPopulatedByMiddleware()
        //{
        //    var context = new DefaultHttpContext();
        //    context.Request.Scheme = HttpRequestScheme;
        //    context.Request.Host = this.httpRequestHost;
        //    context.Request.Path = this.httpRequestPath;
        //    context.Request.QueryString = this.httpRequestQueryString;

        //    middleware.OnBeginRequest(context, 0);
        //    middleware.OnEndRequest(context, 0);

        //    var telemetry = (RequestTelemetry)sentTelemetry;
        //    Assert.NotNull(telemetry.Url);

        //    Assert.Equal(
        //        new Uri(string.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}{3}", HttpRequestScheme, httpRequestHost.Value, httpRequestPath.Value, httpRequestQueryString.Value)),
        //        telemetry.Url);
        //}

        //[Fact]
        //public void RequestWillBeMarkedAsFailedForRunawayException()
        //{
        //    var context = new DefaultHttpContext();
        //    context.Request.Scheme = HttpRequestScheme;
        //    context.Request.Host = this.httpRequestHost;

        //    middleware.OnBeginRequest(context, 0);
        //    middleware.OnDiagnosticsUnhandledException(context, null);
        //    middleware.OnEndRequest(context, 0);

        //    Assert.False(((RequestTelemetry)this.sentTelemetry).Success);
        //}

        //[Fact]
        //public void OnEndRequestSetsRequestNameToMethodAndPathForPostRequest()
        //{
        //    var context = new DefaultHttpContext();
        //    context.Request.Scheme = HttpRequestScheme;
        //    context.Request.Method = "POST";
        //    context.Request.Host = this.httpRequestHost;
        //    context.Request.Path = "/Test";

        //    middleware.OnBeginRequest(context, 0);
        //    middleware.OnEndRequest(context, 0);

        //    var telemetry = (RequestTelemetry)sentTelemetry;

        //    Assert.Equal("POST /Test", telemetry.Name);
        //}

        //[Fact]
        //public void OnEndRequestSetsRequestNameToMethodAndPath()
        //{
        //    var context = new DefaultHttpContext();
        //    context.Request.Scheme = HttpRequestScheme;
        //    context.Request.Method = "GET";
        //    context.Request.Host = this.httpRequestHost;
        //    context.Request.Path = "/Test";

        //    middleware.OnBeginRequest(context, 0);
        //    middleware.OnEndRequest(context, 0);

        //    var telemetry = (RequestTelemetry)sentTelemetry;

        //    Assert.Equal("GET /Test", telemetry.Name);
        //}
    }
}
