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
    using EndpointInternalContext = Microsoft.Developer.Analytics.DataCollection.Model.v2.InternalContextData;
    using JsonConvert = Newtonsoft.Json.JsonConvert;

    [TestClass]
    public class InternalContextTests
    {
        [TestMethod]
        public void SdkVersionIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var context = new InternalContext(new Dictionary<string, string>());
            Assert.Null(context.SdkVersion);
        }

        [TestMethod]
        public void IpCanBeChangedByUserToSpecifyACustomValue()
        {
            var context = new InternalContext(new Dictionary<string, string>());
            context.SdkVersion = "0.0.11.00.1";
            Assert.Equal("0.0.11.00.1", context.SdkVersion);
        }
    }
}
