namespace Microsoft.ApplicationInsights.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.W3C;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HeaderCollectionManipulationTests
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
            Assert.AreEqual("appId=appIdValue,roleName=workerRole", headers["Request-Context"]);

            // overwritting existing key
            headers.SetNameValueHeaderValue("Request-Context", "roleName", "webRole");
            headers.SetNameValueHeaderValue("Request-Context", "appId", "udpatedAppId");
            Assert.AreEqual(1, headers.Keys.Count);
            Assert.AreEqual("roleName=webRole,appId=udpatedAppId", headers["Request-Context"]);
        }

        /// <summary>
        /// Tests that GetCollectionFromHeaderNoDuplicates gets collection of key-value pairs from existing, non-empty header.
        /// </summary>
        [TestMethod]
        public void GetCollectionFromHeaderNoDuplicates()
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers["Correlation-Context"] = "k1=v1, k2=v2, k3 = v3 ";

            var correlationContext = headers.GetNameValueCollectionFromHeader("Correlation-Context");
            Assert.IsNotNull(correlationContext);
            var corrContextDict = correlationContext.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Assert.AreEqual(3, corrContextDict.Count);
            Assert.AreEqual("v1", corrContextDict["k1"]);
            Assert.AreEqual("v2", corrContextDict["k2"]);
            Assert.AreEqual("v3", corrContextDict["k3"]);
        }

        /// <summary>
        /// Tests that GetCollectionFromHeaderNoDuplicates returns null if header does not exists or is empty.
        /// </summary>
        [TestMethod]
        public void GetCollectionFromEmptyHeader()
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            Assert.IsNull(headers.GetNameValueCollectionFromHeader("Correlation-Context"));

            headers["Correlation-Context"] = "   ";
            var correlationContext = headers.GetNameValueCollectionFromHeader("Correlation-Context");
            Assert.IsTrue(correlationContext == null || !correlationContext.Any());
        }

        /// <summary>
        /// Tests that GetCollectionFromHeaderNoDuplicates gets collection of key-value pairs from existing, non-empty header with duplicates.
        /// </summary>
        [TestMethod]
        public void GetCollectionFromHeaderWithDuplicates()
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers["Correlation-Context"] = "k1=v1, k2=v2, k1 = v3";

            var correlationContext = headers.GetNameValueCollectionFromHeader("Correlation-Context").ToArray();
            Assert.AreEqual(2, correlationContext.Length);
            Assert.IsTrue(correlationContext.Contains(new KeyValuePair<string, string>("k1", "v1")));
            Assert.IsTrue(correlationContext.Contains(new KeyValuePair<string, string>("k2", "v2")));
        }

        /// <summary>
        /// Tests that GetCollectionFromHeaderNoDuplicates gets collection of key-value pairs from existing
        /// non-empty invalid header.
        /// </summary>
        [TestMethod]
        public void GetCollectionFromInvalidHeader()
        {
            WebHeaderCollection headers = new WebHeaderCollection();

            // no valid items
            headers["Correlation-Context"] = "k1, some string,k2=v2=v3";

            var correlationContext = headers.GetNameValueCollectionFromHeader("Correlation-Context");
            Assert.IsTrue(correlationContext == null || !correlationContext.Any());

            // some valid items
            headers["Correlation-Context"] = "k1=v1, some string";

            correlationContext = headers.GetNameValueCollectionFromHeader("Correlation-Context");
            Assert.IsNotNull(correlationContext);
            Assert.AreEqual(1, correlationContext.Count());
            Assert.IsTrue(correlationContext.Contains(new KeyValuePair<string, string>("k1", "v1")));
        }

        [TestMethod]
        public void SetNameValueHeaderWithEmptyCollectionSetsNothing()
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.SetHeaderFromNameValueCollection("Correlation-Context", new List<KeyValuePair<string, string>>());
            Assert.IsNull(headers["Correlation-Context"]);
        }

        [TestMethod]
        public void SetNameValueHeaderWithNonEmptyCollectionSetsHeader()
        {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.SetHeaderFromNameValueCollection(
                "Correlation-Context",
                new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("k1 ", "v1"),
                    new KeyValuePair<string, string>("k2", " v2"),
                    new KeyValuePair<string, string>("k1", "v3")
                });
            Assert.IsNotNull(headers["Correlation-Context"]);
            Assert.AreEqual("k1=v1,k2=v2,k1=v3", headers["Correlation-Context"]);
        }

#pragma warning disable 612, 618
        [TestMethod]
        public void GetHeaderValueNoMax()
        {
            WebHeaderCollection headers = new WebHeaderCollection { [W3CConstants.TraceStateHeader] = "k1=v1,k2=v2" };
            var values = headers.GetHeaderValue(W3CConstants.TraceStateHeader)?.ToList();
            Assert.IsNotNull(values);
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual("k1=v1", values.First());
            Assert.AreEqual("k2=v2", values.Last());
        }

        [Xunit.Theory]
        [Xunit.InlineData(12)] // k1=v1,k2=v2,".Length
        [Xunit.InlineData(11)] // k1=v1,k2=v2".Length
        [Xunit.InlineData(15)] // k1=v1,k2=v2,k3=".Length
        [Xunit.InlineData(13)] // k1=v1,k2=v2,k".Length
        public void GetHeaderValueMaxLenTruncatesEnd(int maxLength)
        {
            WebHeaderCollection headers = new WebHeaderCollection { [W3CConstants.TraceStateHeader] = "k1=v1,k2=v2,k3=v3,k4=v4" };
            var values = headers.GetHeaderValue(W3CConstants.TraceStateHeader, maxLength)?.ToList();
            Assert.IsNotNull(values);
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual("k1=v1", values.First());
            Assert.AreEqual("k2=v2", values.Last());
        }

        [Xunit.Theory]
        [Xunit.InlineData(0)]
        [Xunit.InlineData(3)]
        public void GetHeaderValueMaxLenTruncatesEndInvalid(int maxLength)
        {
            WebHeaderCollection headers = new WebHeaderCollection { [W3CConstants.TraceStateHeader] = "k1=v1,k2=v2" };
            var values = headers.GetHeaderValue(W3CConstants.TraceStateHeader, maxLength)?.ToList();
            Assert.IsNull(values);
        }

        [TestMethod]
        public void GetHeaderValueMaxItemsTruncatesEnd()
        {
            WebHeaderCollection headers = new WebHeaderCollection { [W3CConstants.TraceStateHeader] = "k1=v1,k2=v2,k3=v3,k4=v4" };
            var values = headers.GetHeaderValue(W3CConstants.TraceStateHeader, 100500, 2)?.ToList();
            Assert.IsNotNull(values);
            Assert.AreEqual(2, values.Count);
            Assert.AreEqual("k1=v1", values.First());
            Assert.AreEqual("k2=v2", values.Last());
        }

#pragma warning restore 612, 618
    }
}
