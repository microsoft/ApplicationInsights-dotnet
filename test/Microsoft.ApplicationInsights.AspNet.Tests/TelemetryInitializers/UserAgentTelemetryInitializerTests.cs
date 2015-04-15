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

    public class UserAgentTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new UserAgentTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new UserAgentTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new UserAgentTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }
                
        [Fact]
        public void InitializeSetsUserAgentFromHeader()
        {
            var requestTelemetry = new RequestTelemetry();
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            ac.HttpContext.Request.Headers.Add("User-Agent", new[] { "test" });
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new UserAgentTelemetryInitializer(ac);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("test", requestTelemetry.Context.User.UserAgent);
        }

        [Fact]
        public void InitializeDoesNotOverrideUserAgentProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.UserAgent = "Inline";
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            ac.HttpContext.Request.Headers.Add("User-Agent", new[] { "test" });
            var serviceProvider = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new UserAgentTelemetryInitializer(ac);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Inline", requestTelemetry.Context.User.UserAgent);
        }
    }
}