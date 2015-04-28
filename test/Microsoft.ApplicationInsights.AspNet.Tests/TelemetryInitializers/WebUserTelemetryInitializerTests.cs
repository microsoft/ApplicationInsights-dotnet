namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Xunit;

    public class WebUserTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new WebUserTelemetryInitializer(null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new WebUserTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new WebUserTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetsUserFromCookie()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers["Cookie"] = "ai_user=test|2015-04-09T21:51:59.993Z";
            var initializer = new WebUserTelemetryInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("test", requestTelemetry.Context.User.Id);
            Assert.Equal(DateTimeOffset.Parse("2015-04-09T21:51:59.993Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), requestTelemetry.Context.User.AcquisitionDate.Value);
        }

        [Fact]
        public void InitializeDoesNotOverrideUserProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.Id = "Inline";
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers["Cookie"] = "ai_user=test|2015-04-09T21:51:59.993Z";
            var initializer = new WebUserTelemetryInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Inline", requestTelemetry.Context.User.Id);
        }

        [Fact]
        public void InitializeDoesNotThrowOnMalformedUserCookie()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers["Cookie"] = "ai_user=test";
            var initializer = new WebUserTelemetryInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);

            Assert.Equal(null, requestTelemetry.Context.User.Id);
        }

        [Fact]
        public void InitializeDoesNotNotThrowOnMalformedAcquisitionDate()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers["Cookie"] = "ai_user=test|malformeddate";
            var initializer = new WebUserTelemetryInitializer(contextAccessor);

            initializer.Initialize(requestTelemetry);
            Assert.Equal(null, requestTelemetry.Context.User.Id);
            Assert.Equal(false, requestTelemetry.Context.User.AcquisitionDate.HasValue);
        }
    }
}