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

    public class UserAgentTelemetryInitializerTests
    {
        [Fact]
        public void InitializeDoesNotThrowIfHttpContextHolderIsUnavailable()
        {
            var initializer = new UserAgentTelemetryInitializer(new TestServiceProvider());

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var serviceProvider = new TestServiceProvider(new List<object>() { new HttpContextHolder() });
            var initializer = new UserAgentTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder });
            var initializer = new UserAgentTelemetryInitializer(serviceProvider);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetsUserAgentFromHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            contextHolder.Context.Request.Headers.Add("User-Agent", new[] { "test" });
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new UserAgentTelemetryInitializer(serviceProvider);
            

            initializer.Initialize(requestTelemetry);

            Assert.Equal("test", requestTelemetry.Context.User.UserAgent);
        }

        [Fact]
        public void InitializeDoesNotOverrideUserAgentProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.UserAgent = "Inline";
            var contextHolder = new HttpContextHolder();
            contextHolder.Context = new DefaultHttpContext();
            contextHolder.Context.Request.Headers.Add("User-Agent", new[] { "test" });
            var serviceProvider = new TestServiceProvider(new List<object>() { contextHolder, requestTelemetry });
            var initializer = new UserAgentTelemetryInitializer(serviceProvider);


            initializer.Initialize(requestTelemetry);

            Assert.Equal("Inline", requestTelemetry.Context.User.UserAgent);
        }
    }
}