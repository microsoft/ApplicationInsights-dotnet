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

    public class WebClientIpHeaderTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextHolderIsUnavailable()
        {
            var initializer = new WebClientIpHeaderTelemetryInitializer(new TestServiceProvider());

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var serviceProvider = new TestServiceProvider(new List<object>() { new HttpContextHolder() });
            var initializer = new WebClientIpHeaderTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder });
            var initializer = new WebClientIpHeaderTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetsIPFromStandardHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            contextHolder.Context.Request.Headers.Add("X-Forwarded-For", new string[] { "127.0.0.3" });
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new WebClientIpHeaderTelemetryInitializer(serviceProvider);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("127.0.0.3", requestTelemetry.Context.Location.Ip);
        }

        [Fact]
        public void InitializeSetsIPFromCustomHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            contextHolder.Context.Request.Headers.Add("HEADER", new string[] { "127.0.0.3;127.0.0.4" });
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new WebClientIpHeaderTelemetryInitializer(serviceProvider);
            initializer.HeaderNames.Add("HEADER");
            initializer.HeaderValueSeparators = ",;";

            initializer.Initialize(requestTelemetry);

            Assert.Equal("127.0.0.3", requestTelemetry.Context.Location.Ip);
        }

        [Fact]
        public void InitializeDoesNotOverrideIPProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Location.Ip = "127.0.0.4";
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            contextHolder.Context.Request.Headers.Add("X-Forwarded-For", new string[] { "127.0.0.3" });
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new WebClientIpHeaderTelemetryInitializer(serviceProvider);


            initializer.Initialize(requestTelemetry);

            Assert.Equal("127.0.0.4", requestTelemetry.Context.Location.Ip);
        }


    }
}