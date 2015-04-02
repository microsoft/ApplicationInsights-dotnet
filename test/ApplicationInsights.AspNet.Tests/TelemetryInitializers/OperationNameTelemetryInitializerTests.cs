namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.Implementation;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http.Core;
    using System.Collections.Generic;
    using Xunit;

    public class OperationNameTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextHolderIsUnavailable()
        {
            var initializer = new OperationNameTelemetryInitializer(new TestServiceProvider());

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var serviceProvider = new TestServiceProvider(new List<object>() { new HttpContextHolder() });
            var initializer = new OperationNameTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder });
            var initializer = new OperationNameTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationNameProvidedInline()
        {
            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Name = "Name";

            var contextHolder = new HttpContextHolder() { Context = new DefaultHttpContext() };
            
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, new RequestTelemetry() });
            var initializer = new OperationNameTelemetryInitializer(serviceProvider);

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
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = request.HttpContext;

            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, new RequestTelemetry() });
            var initializer = new OperationNameTelemetryInitializer(serviceProvider);
            
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
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = request.HttpContext;

            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, new RequestTelemetry() });
            var initializer = new OperationNameTelemetryInitializer(serviceProvider);

            initializer.Initialize(telemetry);

            Assert.Equal("GET /Test", telemetry.Name);
        }
    }
}