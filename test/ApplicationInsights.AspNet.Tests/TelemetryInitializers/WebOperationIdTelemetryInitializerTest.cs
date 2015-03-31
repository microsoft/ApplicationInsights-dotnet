namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.Implementation;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http.Core;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class WebOperationIdTelemetryInitializerTest
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextHolderIsUnavailable()
        {
            var initializer = new WebOperationIdTelemetryInitializer(new TestServiceProvider());

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var serviceProvider = new TestServiceProvider(new List<object>() { new HttpContextHolder() });
            var initializer = new WebOperationIdTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder });
            var initializer = new WebOperationIdTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotOverrideOperationIdProvidedInline()
        {
            var telemetry = new EventTelemetry();
            telemetry.Context.Operation.Id = "123";
            var requestTelemetry = new RequestTelemetry();
            var contextHolder = new HttpContextHolder() { Context = new DefaultHttpContext() };

            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new WebOperationIdTelemetryInitializer(serviceProvider);

            initializer.Initialize(telemetry);

            Assert.Equal("123", telemetry.Context.Operation.Id);
        }

        [Fact]
        public void InitializeSetsTelemetryOperationIdToRequestId()
        {
            var telemetry = new EventTelemetry();
            var requestTelemetry = new RequestTelemetry();
            var contextHolder = new HttpContextHolder() { Context = new DefaultHttpContext() };
            
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new WebOperationIdTelemetryInitializer(serviceProvider);

            initializer.Initialize(telemetry);

            Assert.Equal(requestTelemetry.Id, telemetry.Context.Operation.Id);
        }
    }
}