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

        private static void AssertDependencyTelemetry(ITelemetry telemetry, Uri requestUri)
        {
            
        }

        private static void AssertRequestHeaderValue(HttpRequestMessage request, string headerName)
        {
            Assert.False(request.Headers.Contains(headerName));
        }

        private static void AssertRequest(HttpRequestMessage request)
        {
            Assert.True(request.Headers.Contains(RequestResponseHeaders.SourceInstrumentationKeyHeader));
            string sourceInstrumentationKeyHeaderValue = request.Headers.GetValues(RequestResponseHeaders.SourceInstrumentationKeyHeader).Single();
            Assert.Equal("0KNjBVW77H/AWpjTEcI7AP0atNgpasSkEll22AtqaVk=", sourceInstrumentationKeyHeaderValue);

            Assert.False(request.Headers.Contains(RequestResponseHeaders.TargetInstrumentationKeyHeader));

            // We can't check this header value because it changes every time the test is run.
            Assert.True(request.Headers.Contains(RequestResponseHeaders.StandardParentIdHeader));

            Assert.False(request.Headers.Contains(RequestResponseHeaders.StandardRootIdHeader));
        }

        private static void AssertTelemetry(ITelemetry telemetry, Uri requestUri, string expectedTarget = null, string expectedType = null, bool? expectedSuccess = null, string expectedResultCode = null)
        {
            if (expectedTarget == null)
            {
                expectedTarget = requestUri.Host;
            }

            if (expectedType == null)
            {
                expectedType = "Http";
            }

            if (expectedSuccess == null)
            {
                expectedSuccess = true;
            }

            if (expectedResultCode == null)
            {
                expectedResultCode = "200";
            }

            Assert.NotNull(telemetry);
            Assert.IsType<DependencyTelemetry>(telemetry);
            DependencyTelemetry dependencyTelemetry = telemetry as DependencyTelemetry;
            Assert.Equal("GET " + requestUri.AbsolutePath, dependencyTelemetry.Name);
            Assert.Equal(expectedTarget, dependencyTelemetry.Target);
            Assert.Equal(requestUri.OriginalString, dependencyTelemetry.Data);
            Assert.Equal(expectedType, dependencyTelemetry.Type);
            Assert.Equal(expectedSuccess.Value, dependencyTelemetry.Success);
            Assert.Equal(expectedResultCode, dependencyTelemetry.ResultCode);
            Assert.Equal("dotnet:2.2.0-54036", dependencyTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void OnRequestSentAndResponseReceived()
        {
            Guid loggingId = new Guid();
            Uri requestUri = new Uri("http://www.microsoft.com/test/path.html");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            listener.OnRequestSent(request, loggingId, 0);

            Assert.Null(this.sentTelemetry); // Telemetry shouldn't be logged until OnResponseReceived().
            AssertRequest(request);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            listener.OnResponseReceived(response, loggingId, 1);

            AssertTelemetry(this.sentTelemetry, requestUri);
        }

        [Fact]
        public void OnRequestSentAndResponseReceivedWhereTargetInstrumentationKeyIsSameAsSource()
        {
            Guid loggingId = new Guid();
            Uri requestUri = new Uri("http://www.microsoft.com/test/path.html");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            listener.OnRequestSent(request, loggingId, 0);

            AssertRequest(request);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add(RequestResponseHeaders.TargetInstrumentationKeyHeader, request.Headers.GetValues(RequestResponseHeaders.SourceInstrumentationKeyHeader).Single());
            listener.OnResponseReceived(response, loggingId, 1);

            AssertTelemetry(this.sentTelemetry, requestUri);
        }

        [Fact]
        public void OnRequestSentAndResponseReceivedWhereTargetInstrumentationKeyIsNotSameAsSource()
        {
            Guid loggingId = new Guid();
            Uri requestUri = new Uri("http://www.microsoft.com/test/path.html");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            listener.OnRequestSent(request, loggingId, 0);

            Assert.Null(this.sentTelemetry); // Telemetry shouldn't be logged until OnResponseReceived().

            AssertRequest(request);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add(RequestResponseHeaders.TargetInstrumentationKeyHeader, "DIFFERENT_IKEY_HASH");
            listener.OnResponseReceived(response, loggingId, 1);

            AssertTelemetry(this.sentTelemetry, requestUri, expectedTarget: requestUri.Host + " | DIFFERENT_IKEY_HASH", expectedType: "Application Insights");
        }

        [Fact]
        public void OnRequestSentAndResponseReceivedWith500StatusCode()
        {
            Guid loggingId = new Guid();
            Uri requestUri = new Uri("http://www.microsoft.com/test/path.html");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            listener.OnRequestSent(request, loggingId, 0);

            Assert.Null(this.sentTelemetry); // Telemetry shouldn't be logged until OnResponseReceived().

            AssertRequest(request);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            listener.OnResponseReceived(response, loggingId, 1);

            AssertTelemetry(this.sentTelemetry, requestUri, expectedSuccess: false, expectedResultCode: "500");
        }

        [Fact]
        public void OnResponseReceivedWithNoRequestSent()
        {
            listener.OnResponseReceived(new HttpResponseMessage(HttpStatusCode.OK), new Guid(), 1);
            Assert.Null(this.sentTelemetry); // If there wasn't an associated request sent, then OnResponseReceived() shouldn't do anything.
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
