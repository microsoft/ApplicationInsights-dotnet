
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
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var config = new ConfigurationBuilder().AddJsonFile("project.json").Build();

            var initializer = new ComponentVersionTelemetryInitializer(config);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var config = new ConfigurationBuilder().AddJsonFile("project.json").Build();

            var initializer = new ComponentVersionTelemetryInitializer(config);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeConfigurationWithNullReturnsNull()
        {
            var requestTelemetry = new RequestTelemetry();
            
            var initializer = new ComponentVersionTelemetryInitializer(null);

            initializer.Initialize(requestTelemetry);

            Assert.Null(requestTelemetry.Context.Component.Version);
        }
    }
}
