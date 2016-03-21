
namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using System;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Xunit;
    using Microsoft.AspNet.Http.Internal;
    using Microsoft.Extensions.Configuration;

    public class ComponentVersionTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new ComponentVersionTelemetryInitializer(null, null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };
            var config = new ConfigurationBuilder().AddJsonFile("project.json").Build();

            var initializer = new ComponentVersionTelemetryInitializer(ac, config);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };
            var config = new ConfigurationBuilder().AddJsonFile("project.json").Build();

            var initializer = new ComponentVersionTelemetryInitializer(ac, config);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetsGetVersionInformationIsNotNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            var config = new ConfigurationBuilder().AddJsonFile("project.json").Build();

            var initializer = new ComponentVersionTelemetryInitializer(contextAccessor, config);

            initializer.Initialize(requestTelemetry);

            Assert.NotNull(requestTelemetry.Context.Component.Version);
        }

        [Fact]
        public void InitializeConfigurationWithNullReturnsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            
            var initializer = new ComponentVersionTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Component.Version);
        }
    }
}
