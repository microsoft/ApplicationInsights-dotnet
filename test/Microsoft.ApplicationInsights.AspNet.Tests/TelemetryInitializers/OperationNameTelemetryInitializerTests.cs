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

    public class OperationNameTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new OperationNameTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationNameProvidedInline()
        {
            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Name = "Name";
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { new RequestTelemetry() });
            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(telemetry);

            Assert.Equal("Name", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationNameToMethodAndPath()
        {
            var telemetry = new EventTelemetry();
            var request = new DefaultHttpContext().Request;
            request.Method = "GET";
            request.Path = new Microsoft.AspNet.Http.PathString("/Test");
            var ac = new HttpContextAccessor() { HttpContext = request.HttpContext };
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { new RequestTelemetry() });
            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(telemetry);

            Assert.Equal("GET /Test", telemetry.Context.Operation.Name);
        }

        [Fact]
        public void InitializeSetsRequestNameToMethodAndPath()
        {
            var telemetry = new RequestTelemetry();
            var request = new DefaultHttpContext().Request;
            request.Method = "GET";
            request.Path = new Microsoft.AspNet.Http.PathString("/Test");
            var ac = new HttpContextAccessor() { HttpContext = request.HttpContext };
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { new RequestTelemetry() });
            var initializer = new OperationNameTelemetryInitializer(ac);

            initializer.Initialize(telemetry);

            Assert.Equal("GET /Test", telemetry.Name);
        }
    }
}