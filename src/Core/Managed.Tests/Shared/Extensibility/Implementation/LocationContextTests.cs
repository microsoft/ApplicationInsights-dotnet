namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    
    using Assert = Xunit.Assert;
    using EndpointLocationContext = Microsoft.Developer.Analytics.DataCollection.Model.v2.LocationContextData;
    using JsonConvert = Newtonsoft.Json.JsonConvert;
    
    [TestClass]
    public class LocationContextTests
    {
        [TestMethod]
        public void ClassIsPublicToAllowSpecifyingCustomLocationContextPropertiesInUserCode()
        {
            Assert.True(typeof(LocationContext).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void IpIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var context = new LocationContext(new Dictionary<string, string>());
            Assert.Null(context.Ip);
        }

        [TestMethod]
        public void IpCanBeChangedByUserToSpecifyACustomValue()
        {
            var context = new LocationContext(new Dictionary<string, string>());
            context.Ip = "192.168.1.1";
            Assert.Equal("192.168.1.1", context.Ip);
        }

        [TestMethod]
        public void IpRejectsNonIpv4Address()
        {
            var context = new LocationContext(new Dictionary<string, string>());
            context.Ip = "2401:4893:f0:5c:2452:4474:03d2:9375";
            Assert.Null(context.Ip);
        }
    }
}
