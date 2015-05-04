namespace Microsoft.ApplicationInsights.AspNet.Tests.ContextInitializers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;

    public class DomainNameRoleInstanceContextInitializerTests
    {
        [Fact]
        public void RoleInstanceNameIsSetToDomainAndHost()
        {
            var source = new DomainNameRoleInstanceContextInitializer();
            var telemetryContext = new TelemetryContext();

            source.Initialize(telemetryContext);
            
            string hostName = Dns.GetHostName();

#if !dnxcore50
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase) == false)
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }
#endif

            Assert.Equal(hostName, telemetryContext.Device.RoleInstance);
        }

        [Fact]
        public void ContextInitializerDoesNotOverrideMachineName()
        {
            var source = new DomainNameRoleInstanceContextInitializer();
            var telemetryContext = new TelemetryContext();
            telemetryContext.Device.RoleInstance = "Test";

            source.Initialize(telemetryContext);

            Assert.Equal("Test", telemetryContext.Device.RoleInstance);
        }
    }
}