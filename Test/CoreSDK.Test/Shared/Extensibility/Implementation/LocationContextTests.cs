namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using Assert = Xunit.Assert;
    
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
            var context = new LocationContext();
            Assert.Null(context.Ip);
        }

        [TestMethod]
        public void IpCanBeChangedByUserToSpecifyACustomValue()
        {
            var context = new LocationContext();
            context.Ip = "192.168.1.1";
            Assert.Equal("192.168.1.1", context.Ip);
        }
    }
}
