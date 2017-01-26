
namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    using System;
    using System.Globalization;
    using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    public class RequestTrackingMiddlewareTest
    {
        private const string scheme = "http";
        private static readonly HostString host = new HostString("testHost");
        private static readonly PathString path = new PathString("/path/path");
        private static readonly QueryString query = new QueryString("?query=1");

        private static Uri CreateUri(string scheme, HostString host, PathString? path = null, QueryString? query = null)
        {
            string uriString = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", scheme, host);
            if (path != null)
            {
                uriString += path.Value;
            }
            if (query != null)
            {
                uriString += query.Value;
            }
            return new Uri(uriString);
        }

        private HttpContext CreateContext(string scheme, HostString host, PathString? path = null, QueryString? query = null, string method = null)
        {
            HttpContext context = new DefaultHttpContext();
            context.Request.Scheme = scheme;
            context.Request.Host = host;

            if (path.HasValue)
            {
                context.Request.Path = path.Value;
            }

            if (query.HasValue)
            {
                context.Request.QueryString = query.Value;
            }

            if (!string.IsNullOrEmpty(method))
            {
                context.Request.Method = method;
            }

            Assert.Null(context.Features.Get<RequestTelemetry>());

            return context;
        }

        private ITelemetry sentTelemetry;

        private readonly HostingDiagnosticListener middleware;

        public RequestTrackingMiddlewareTest()
        {
            this.middleware = new HostingDiagnosticListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = telemetry));
        }

        [Fact]
        public void TestSdkVersionIsPopulatedByMiddleware()
        {
            HttpContext context = CreateContext(scheme, host);

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnEndRequest(context, 0);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            Assert.Equal("", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void TestRequestUriIsPopulatedByMiddleware()
        {
            HttpContext context = CreateContext(scheme, host, path, query);

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnEndRequest(context, 0);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            Assert.Equal("", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host, path, query), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void RequestWillBeMarkedAsFailedForRunawayException()
        {
            HttpContext context = CreateContext(scheme, host);

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnDiagnosticsUnhandledException(context, null);
            middleware.OnEndRequest(context, 0);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.False(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            Assert.Equal("", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
        }

        [Fact]
        public void OnEndRequestSetsRequestNameToMethodAndPathForPostRequest()
        {
            HttpContext context = CreateContext(scheme, host, "/Test", method: "POST");

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnEndRequest(context, 0);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            Assert.Equal("POST", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("POST /Test", requestTelemetry.Name);
        }

        [Fact]
        public void OnEndRequestSetsRequestNameToMethodAndPath()
        {
            HttpContext context = CreateContext(scheme, host, "/Test", method: "GET");

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnEndRequest(context, 0);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            Assert.Equal("GET", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("GET /Test", requestTelemetry.Name);
        }

        [Fact]
        public void OnEndRequestFromSameInstrumentationKey()
        {
            HttpContext context = CreateContext(scheme, host, "/Test", method: "GET");
            context.Request.Headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, CommonMocks.InstrumentationKeyHash);

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnEndRequest(context, 0);
            
            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("", requestTelemetry.Source);
            Assert.Equal("GET", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("GET /Test", requestTelemetry.Name);
        }

        [Fact]
        public void OnEndRequestFromDifferentInstrumentationKey()
        {
            HttpContext context = CreateContext(scheme, host, "/Test", method: "GET");
            context.Request.Headers.Add(RequestResponseHeaders.SourceInstrumentationKeyHeader, "DIFFERENT_INSTRUMENTATION_KEY_HASH");

            middleware.OnBeginRequest(context, 0);

            Assert.NotNull(context.Features.Get<RequestTelemetry>());
            Assert.Equal(context.Response.Headers[RequestResponseHeaders.TargetInstrumentationKeyHeader], CommonMocks.InstrumentationKeyHash);

            middleware.OnEndRequest(context, 0);

            Assert.NotNull(this.sentTelemetry);
            Assert.IsType<RequestTelemetry>(this.sentTelemetry);
            RequestTelemetry requestTelemetry = this.sentTelemetry as RequestTelemetry;
            Assert.True(requestTelemetry.Duration.TotalMilliseconds >= 0);
            Assert.True(requestTelemetry.Success);
            Assert.Equal(CommonMocks.InstrumentationKey, requestTelemetry.Context.InstrumentationKey);
            Assert.Equal("DIFFERENT_INSTRUMENTATION_KEY_HASH", requestTelemetry.Source);
            Assert.Equal("GET", requestTelemetry.HttpMethod);
            Assert.Equal(CreateUri(scheme, host, "/Test"), requestTelemetry.Url);
            Assert.NotEmpty(requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Contains(SdkVersionTestUtils.VersionPrefix, requestTelemetry.Context.GetInternalContext().SdkVersion);
            Assert.Equal("GET /Test", requestTelemetry.Name);
        }
    }
}
