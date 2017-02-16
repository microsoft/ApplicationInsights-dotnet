namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TelemetryInitializers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Xunit;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.AspNetCore.Http;

    public class OperationCorrelationTelemetryInitializerTests
    {
        private static OperationCorrelationTelemetryInitializer CreateInitializer(RequestTelemetry requestTelemetry)
        {
            return CreateInitializer(HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry));
        }

        private static OperationCorrelationTelemetryInitializer CreateInitializer(HttpContext httpContext)
        {
            return CreateInitializer(new HttpContextAccessor() { HttpContext = httpContext });
        }

        private static OperationCorrelationTelemetryInitializer CreateInitializer(IHttpContextAccessor contextAccessor)
        {
            return new OperationCorrelationTelemetryInitializer(contextAccessor);
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
        public void InitializeSetsRequestOperationIdIfNotAlreadySet()
        {
            var requestTelemetry = new RequestTelemetry();
            var initializer = CreateInitializer(requestTelemetry);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal(requestTelemetry.Id, requestTelemetry.Context.Operation.Id);
        }

        [Fact]
        public void InitializeSetsOperationIdAndParentIdToRequestId()
        {
            var requestTelemetry = new RequestTelemetry();
            var initializer = CreateInitializer(requestTelemetry);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal(requestTelemetry.Id, telemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, telemetry.Context.Operation.ParentId);
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
        public void InitializeSetsOperationIdToRequestOperationIdAndParentIdToRequestId()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Operation.Id = "ABC";
            var initializer = CreateInitializer(requestTelemetry);

            var telemetry = new EventTelemetry();
            initializer.Initialize(telemetry);

            Assert.Equal("ABC", telemetry.Context.Operation.Id);
            Assert.Equal(requestTelemetry.Id, telemetry.Context.Operation.ParentId);
        }
    }
}
