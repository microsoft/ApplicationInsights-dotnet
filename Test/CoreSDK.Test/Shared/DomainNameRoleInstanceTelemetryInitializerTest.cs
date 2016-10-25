namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    
    [TestClass]
    public class DomainNameRoleInstanceContextInitializerTest
    {
        [TestMethod]
        public void RoleInstanceNameIsSetToDomainAndHost()
        {
            var telemetryItem = new EventTelemetry();
            var source = new DomainNameRoleInstanceTelemetryInitializer();
            
            source.Initialize(telemetryItem);

            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = Dns.GetHostName();

            if (hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase) == false)
            {
                hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
            }

            Assert.Equal(hostName, telemetryItem.Context.Cloud.RoleInstance);
            Assert.Equal(hostName, telemetryItem.Context.Internal.NodeName);
        }

        [TestMethod]
        public void ContextInitializerDoesNotOverrideMachineName()
        {
            var telemetryItem = new EventTelemetry();
            var source = new DomainNameRoleInstanceTelemetryInitializer();
            telemetryItem.Context.Cloud.RoleInstance = "Test";

            source.Initialize(telemetryItem);

            Assert.Equal("Test", telemetryItem.Context.Cloud.RoleInstance);
        }

        [TestMethod]
        public void ContextInitializerDoesNotOverrideNodeName()
        {
            var telemetryItem = new EventTelemetry();
            var source = new DomainNameRoleInstanceTelemetryInitializer();
            telemetryItem.Context.Internal.NodeName = "Test";

            source.Initialize(telemetryItem);

            Assert.Equal("Test", telemetryItem.Context.Internal.NodeName);
        }
    }
}