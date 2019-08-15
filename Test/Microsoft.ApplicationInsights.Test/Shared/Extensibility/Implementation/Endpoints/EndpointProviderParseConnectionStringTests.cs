namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointProviderParseConnectionStringTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestMaliciousConnectionString()
        {
            new EndpointProvider()
            {
                ConnectionString = new string('*', EndpointProvider.ConnectionStringMaxLength + 1)
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestParseConnectionString_Null()
        {
            EndpointProvider.ParseConnectionString(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestParseConnectionString_Empty()
        {
            EndpointProvider.ParseConnectionString("");
        }

        [TestMethod]
        public void TestParseConnectionString()
        {
            var test = EndpointProvider.ParseConnectionString("key1=value1;key2=value2;key3=value3");

            var expected = new Dictionary<string, string>
            {
                {"key1", "value1" },
                {"key2", "value2" },
                {"key3", "value3" }
            };

            CollectionAssert.AreEqual(expected, test);
        }

        [TestMethod]
        public void TestParseConnectionString_WithTrailingSemicolon()
        {
            var test = EndpointProvider.ParseConnectionString("key1=value1;key2=value2;key3=value3;");

            var expected = new Dictionary<string, string>
            {
                {"key1", "value1" },
                {"key2", "value2" },
                {"key3", "value3" }
            };

            CollectionAssert.AreEqual(expected, test);
        }

        /// <summary>
        /// Users can input unexpected casing in their connection strings.
        /// Verify that we can fetch any value from the dictionary regardless of the casing.
        /// </summary>
        [TestMethod]
        public void VerifyConnectionStringDictionary_IsCaseInsensitive()
        {
            var test = EndpointProvider.ParseConnectionString("UPPERCASE=value1;lowercase=value2;MixedCase=value3");

            var expected = new Dictionary<string, string>
            {
                {"UPPERCASE", "value1" },
                {"lowercase", "value2" },
                {"MixedCase", "value3" }
            };

            CollectionAssert.AreEqual(expected, test);

            Assert.AreEqual("value1", test["UPPERCASE"]);
            Assert.AreEqual("value1", test["uppercase"]);
            Assert.AreEqual("value2", test["LOWERCASE"]);
            Assert.AreEqual("value2", test["lowercase"]);
            Assert.AreEqual("value3", test["MIXEDCASE"]);
            Assert.AreEqual("value3", test["mixedcase"]);
        }

        [TestMethod]
        [ExpectedException(typeof(ConnectionStringDuplicateKeyException))]
        public void TestParseConnectionString_DuplaceKeys()
        {
            EndpointProvider.ParseConnectionString("key1=value1;key1=value2");
        }

        [TestMethod]
        [ExpectedException(typeof(ConnectionStringInvalidDelimiterException))]
        public void TestParseConnectionString_InvalidDelimiters()
        {
            EndpointProvider.ParseConnectionString("key1;key2=value2");
        }

        [TestMethod]
        [ExpectedException(typeof(ConnectionStringInvalidDelimiterException))]
        public void TestParseConnectionString_InvalidCharInValue()
        {
            EndpointProvider.ParseConnectionString("key1=value1=value2");
        }
    }
}
