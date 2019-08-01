﻿namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointProviderParseConnectionStringTests
    {
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

        /// <summary>
        /// Users can input unexpected casing in their connection strings.
        /// Verify that we can fetch any value from the dictionary regardless of the casing.
        /// </summary>
        [TestMethod]
        public void VerifyConnectionStringDictionaryIsCaseInsensitive()
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
        [ExpectedException(typeof(ArgumentException))]
        public void TestParseConnectionString_DuplaceKeys()
        {
            var test = EndpointProvider.ParseConnectionString("key1=value1;key1=value2");
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestParseConnectionString_InvalidString()
        {
            var test = EndpointProvider.ParseConnectionString("key1;value1=key2=value2=key3=value3");
        }
    }
}
