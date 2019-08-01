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
