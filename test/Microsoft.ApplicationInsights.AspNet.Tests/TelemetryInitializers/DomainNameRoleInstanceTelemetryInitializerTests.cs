namespace Microsoft.ApplicationInsights.AspNet.Tests.ContextInitializers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using Helpers;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Http.Internal;
    using Xunit;

    public class DomainNameRoleInstanceTelemetryInitializerTests
    {
        private const string TestListenerName = "TestListener";

        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var initializer = new DomainNameRoleInstanceTelemetryInitializer(null);
            });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new DomainNameRoleInstanceTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new DomainNameRoleInstanceTelemetryInitializer(ac);

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void RoleInstanceNameIsSetToDomainAndHost()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var source = new DomainNameRoleInstanceTelemetryInitializer(contextAccessor);
            var requestTelemetry = new RequestTelemetry();
            source.Initialize(requestTelemetry);
            
            string hostName = Dns.GetHostName();

#if !dnxcore50
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase) == false)
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }
#endif

            Assert.Equal(hostName, requestTelemetry.Context.Cloud.RoleInstance);
        }

        [Fact]
        public void ContextInitializerDoesNotOverrideMachineName()
        {
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(new RequestTelemetry(), null);
            var source = new DomainNameRoleInstanceTelemetryInitializer(contextAccessor);
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Cloud.RoleInstance = "Test";

            source.Initialize(requestTelemetry);

            Assert.Equal("Test", requestTelemetry.Context.Cloud.RoleInstance);
        }
    }
}