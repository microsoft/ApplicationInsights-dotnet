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

    public class WebSessionTelemetryInitializerTests
	{
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new WebSessionTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { Value = null };

            var initializer = new WebSessionTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { Value = new DefaultHttpContext() };

            var initializer = new WebSessionTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }
                
        [Fact]
        public void InitializeSetsUserFromCookie()
        {
            var requestTelemetry = new RequestTelemetry();
            var ac = new HttpContextAccessor() { Value = new DefaultHttpContext() };
			ac.Value.Request.Headers["Cookie"] = "ai_session=test";
			ac.Value.RequestServices = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new WebSessionTelemetryInitializer(ac);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("test", requestTelemetry.Context.Session.Id);
        }

        [Fact]
        public void InitializeDoesNotOverrideUserProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Session.Id = "Inline";
            var ac = new HttpContextAccessor() { Value = new DefaultHttpContext() };
			ac.Value.Request.Headers["Cookie"] = "ai_session=test";
            var serviceProvider = new TestServiceProvider(new List<object>() { requestTelemetry });
            var initializer = new WebSessionTelemetryInitializer(ac);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Inline", requestTelemetry.Context.Session.Id);
        }
    }
}