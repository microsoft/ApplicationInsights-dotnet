namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http.Core;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class OperationIdTelemetryInitializerTest
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new OperationIdTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new OperationIdTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new OperationIdTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationIdProvidedInline()
        {
            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Id = "123";
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { new RequestTelemetry() });
            var initializer = new OperationIdTelemetryInitializer(ac);

            initializer.Initialize(telemetry);

            Assert.Equal("123", telemetry.Context.Operation.Id);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationIdToRequestId()
        {
            var telemetry = new EventTelemetry();
            var requestTelemetry = new RequestTelemetry();
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new OperationIdTelemetryInitializer(ac);

            initializer.Initialize(telemetry);

            Assert.Equal(requestTelemetry.Id, telemetry.Context.Operation.Id);
        }
    }
}