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

    public class ClientIpHeaderTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new ClientIpHeaderTelemetryInitializer(null);  });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };
            
            var initializer = new ClientIpHeaderTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            
            var initializer = new ClientIpHeaderTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetsIPFromStandardHeader()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var requestTelemetry = new RequestTelemetry();
            ac.HttpContext.Request.Headers.Add("X-Forwarded-For", new string[] { "127.0.0.3" });
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { requestTelemetry });
            
            var initializer = new ClientIpHeaderTelemetryInitializer(ac);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("127.0.0.3", requestTelemetry.Context.Location.Ip);
        }

        [Fact]
        public void InitializeSetsIPFromCustomHeader()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var requestTelemetry = new RequestTelemetry();
            ac.HttpContext.Request.Headers.Add("HEADER", new string[] { "127.0.0.3;127.0.0.4" });
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new ClientIpHeaderTelemetryInitializer(ac);
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
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            ac.HttpContext.Request.Headers.Add("X-Forwarded-For", new string[] { "127.0.0.3" });
            ac.HttpContext.RequestServices = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new ClientIpHeaderTelemetryInitializer(ac);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("127.0.0.4", requestTelemetry.Context.Location.Ip);
        }
    }
}