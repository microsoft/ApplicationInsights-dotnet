namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointContainerTests
    {
        [TestMethod]
        public void VerifyContainer_WithoutConnectionStringShouldReturnDefaultEndpoints()
        {
            var container = new EndpointContainer(new EndpointProvider());

            Assert.AreEqual(Constants.BreezeEndpoint, container.Ingestion.AbsoluteUri);
            Assert.AreEqual(Constants.LiveMetricsEndpoint, container.Live.AbsoluteUri);
            Assert.AreEqual(Constants.ProfilerEndpoint, container.Profiler.AbsoluteUri);
            Assert.AreEqual(Constants.SnapshotEndpoint, container.Snapshot.AbsoluteUri);
        }
    }
}
