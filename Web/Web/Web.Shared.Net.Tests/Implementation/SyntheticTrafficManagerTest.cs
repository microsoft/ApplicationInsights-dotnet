namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SyntheticTrafficManagerTest
    {
        [TestMethod]
        public void IsSyntheticReturnsFalseByDefault()
        {
            var m = new SyntheticTrafficManager();

            Assert.IsFalse(m.IsSynthetic(HttpModuleHelper.GetFakeHttpContext()));
        }

        [TestMethod]
        public void IsSyntheticReturnsTrueWhenTestRunHeaderExists()
        {
            var m = new SyntheticTrafficManager();

            var actual = m.IsSynthetic(HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "SyntheticTest-RunId", "ID" } }));

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void GetSessionIdReturnsNullByDefault()
        {
            var m = new SyntheticTrafficManager();

            Assert.IsNull(m.GetSessionId(HttpModuleHelper.GetFakeHttpContext()));
        }

        [TestMethod]
        public void GetSessionIdReturnsIdFromHeader()
        {
            var m = new SyntheticTrafficManager();

            var actual = m.GetSessionId(HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "SyntheticTest-RunId", "ID" } }));

            Assert.AreEqual("ID", actual);
        }

        [TestMethod]
        public void GetUserIdReturnsNullByDefault()
        {
            var m = new SyntheticTrafficManager();

            Assert.IsNull(m.GetUserId(HttpModuleHelper.GetFakeHttpContext()));
        }

        [TestMethod]
        public void GetUserIdReturnsIdFromHeader()
        {
            var m = new SyntheticTrafficManager();

            var actual = m.GetUserId(HttpModuleHelper.GetFakeHttpContext(new Dictionary<string, string> { { "SyntheticTest-Location", "LOCATION" } }));

            Assert.AreEqual("LOCATION", actual);
        }
    }
}
