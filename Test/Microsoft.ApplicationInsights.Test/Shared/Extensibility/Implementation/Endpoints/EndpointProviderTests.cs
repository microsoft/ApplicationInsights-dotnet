namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointProviderTests
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestParseConnectionString_DuplaceKeys()
        {
            var test = EndpointProvider.ParseConnectionString("key1=value1;key2=value2;key3=value3");
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void TestParseConnectionString_InvalidString()
        {
            var test = EndpointProvider.ParseConnectionString("key1;value1=key2=value2=key3=value3");
        }

        [TestMethod]
        public void TestDefaultEndpoints()
        {
            var endpointThing = new EndpointProvider()
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };

            var breezeTest = endpointThing.GetEndpoint(EndpointName.Breeze);
            Assert.AreEqual(Constants.BreezeEndpoint, breezeTest.AbsoluteUri);

            var liveMetricsTest = endpointThing.GetEndpoint(EndpointName.LiveMetrics);
            Assert.AreEqual(Constants.LiveMetricsEndpoint, liveMetricsTest.AbsoluteUri);

            var profilerTest = endpointThing.GetEndpoint(EndpointName.Profiler);
            Assert.AreEqual(Constants.ProfilerEndpoint, profilerTest.AbsoluteUri);

            var snapshotTest = endpointThing.GetEndpoint(EndpointName.Snapshot);
            Assert.AreEqual(Constants.SnapshotEndpoint, snapshotTest.AbsoluteUri);
        }
    }
}
