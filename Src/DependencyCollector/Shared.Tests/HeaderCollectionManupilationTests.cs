namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using Common;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HeaderCollectionManupilationTests
    {
        /// <summary>
        /// Ensures that the GetNameValueHeaderValue extension methods works as expected.
        /// </summary>
        [TestMethod]
        public void GetNameValueHeaderWorksCorrectly()
        {
            WebHeaderCollection headers = new WebHeaderCollection();

            // header collection empty
            Assert.IsNull(headers.GetNameValueHeaderValue("someName", "someKey"));

            headers.Add("header-one", "value1");
            headers.Add("header-two", "value2");

            // header not found
            Assert.IsNull(headers.GetNameValueHeaderValue("myheader", "key1"));

            // header key not found
            Assert.IsNull(headers.GetNameValueHeaderValue("header-two", "key1"));

            // header should be found. We test cases were there are spaces around delimeters.
            headers.Add("headerThree", "key1=value1, key2=value2");
            headers.Add("Header-Four", "key1=value1,key2=value2,key3=value3");
            headers.Add("Header-Five", "key1=value1,key2 = value2,key3=value3");

            Assert.AreEqual("value2", headers.GetNameValueHeaderValue("headerThree", "key2"));
            Assert.AreEqual("value2", headers.GetNameValueHeaderValue("Header-Four", "key2"));
            Assert.AreEqual("value2", headers.GetNameValueHeaderValue("Header-Five", "key2"));

            // header with key value format but missing key
            Assert.IsNull(headers.GetNameValueHeaderValue("headerThree", "keyX"));
        }

        /// <summary>
        /// Ensures that the SetNameValueHeaderValue works as expected.
        /// </summary>
        [TestMethod]
        public void SetNameValueHeaderWorksCorrectly()
        {
            // Collection empty
            WebHeaderCollection headers = new WebHeaderCollection();

            headers.SetNameValueHeaderValue("Request-Context", "appId", "appIdValue");
            Assert.AreEqual(1, headers.Keys.Count);
            Assert.AreEqual("appId=appIdValue", headers["Request-Context"]);

            // Non empty collection - adding new key
            headers.SetNameValueHeaderValue("Request-Context", "roleName", "workerRole");
            Assert.AreEqual(1, headers.Keys.Count);
            Assert.AreEqual("appId=appIdValue, roleName=workerRole", headers["Request-Context"]);

            // overwritting existing key
            headers.SetNameValueHeaderValue("Request-Context", "roleName", "webRole");
            headers.SetNameValueHeaderValue("Request-Context", "appId", "udpatedAppId");
            Assert.AreEqual(1, headers.Keys.Count);
            Assert.AreEqual("appId=udpatedAppId, roleName=webRole", headers["Request-Context"]);
        }
    }
}
