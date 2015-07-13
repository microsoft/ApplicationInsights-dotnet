namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using EndpointSessionContext = Microsoft.Developer.Analytics.DataCollection.Model.v2.SessionContextData;
    using JsonConvert = Newtonsoft.Json.JsonConvert;

    [TestClass]
    public class SessionContextTest
    {
        [TestMethod]
        public void ClassIsPublicToAllowInstantiationInSdkAndSessionCode()
        {
            Assert.True(typeof(SessionContext).GetTypeInfo().IsPublic);
        }
        
        [TestMethod]
        public void IdIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var session = new SessionContext(new Dictionary<string, string>());
            Assert.Null(session.Id);
        }

        [TestMethod]
        public void IdCanBeChangedByUserToSupplyApplicationDefinedValue()
        {
            var session = new SessionContext(new Dictionary<string, string>());
            session.Id = "42";
            Assert.Equal("42", session.Id);
        }
        
        [TestMethod]
        public void IsFirstIsNullByDefaultToAvoidSendingItToEndpointUnnecessarily()
        {
            var session = new SessionContext(new Dictionary<string, string>());
            Assert.Null(session.IsFirst);
        }

        [TestMethod]
        public void IsFirstCanBeSetByUserToSupplyCustomValue()
        {
            var session = new SessionContext(new Dictionary<string, string>());
            session.IsFirst = true;
            Assert.Equal(true, session.IsFirst);
        }

        [TestMethod]
        public void IsFirstCanBeSetToNullToRemoveItFromJsonPayload()
        {
            var session = new SessionContext(new Dictionary<string, string>()) { IsFirst = true };
            session.IsFirst = null;
            Assert.Null(session.IsFirst);
        }
    }
}
