namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    public class OperationCorrelationTelemetryInitializerTests
    {
        private static OperationCorrelationTelemetryInitializer CreateInitializer(RequestTelemetry requestTelemetry, string appId = null)
        {
            return CreateInitializer(HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry, httpContextCorrelationId: appId));
        }

        private static OperationCorrelationTelemetryInitializer CreateInitializer(HttpContext httpContext)
        {
            return CreateInitializer(new HttpContextAccessor() { HttpContext = httpContext });
        }

        private static OperationCorrelationTelemetryInitializer CreateInitializer(IHttpContextAccessor contextAccessor)
        {
            return new OperationCorrelationTelemetryInitializer(contextAccessor, CommonMocks.MockCorrelationIdLookupHelper());
        }

        [Fact]
        public void ConstructorThrowsWhenHttpContextAccessorIsNull()
        {
            Assert.ThrowsAny<ArgumentNullException>(() => { var initializer = CreateInitializer(contextAccessor: null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var initializer = CreateInitializer(httpContext: null);
            initializer.Initialize(new EventTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var initializer = CreateInitializer(new DefaultHttpContext());
            initializer.Initialize(new EventTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationIdProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            var initializer = CreateInitializer(requestTelemetry);

            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Id = "123";

            initializer.Initialize(telemetry);

            Assert.Equal("123", telemetry.Context.Operation.Id);
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationParentIdProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            var initializer = CreateInitializer(requestTelemetry);

            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.ParentId = "123";

            initializer.Initialize(telemetry);

            Assert.Equal("123", telemetry.Context.Operation.ParentId);
        }

        [Fact]
        public void InitializeDoesNotOverrideSourceProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Source = "TEST_SOURCE";
            var initializer = CreateInitializer(requestTelemetry);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("TEST_SOURCE", requestTelemetry.Source);
        }

        [Fact]
        public void InitializeDoesntAddSourceIfRequestHeadersDontHaveSource()
        {
            var requestTelemetry = new RequestTelemetry();
            var initializer = CreateInitializer(requestTelemetry, "TEST_SOURCE");

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("TEST_SOURCE", requestTelemetry.Source);
        }
    }
}
