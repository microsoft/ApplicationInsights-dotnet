namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Endpoints")]
    public class EndpointControllerTests
    {
        [TestMethod]
        public void VerifyControllerSendsCorrectEnumToProvider()
        {
            var testProvider = new TestProvider();
            var controller = new EndpointController(testProvider);

            _ = controller.Breeze;
            Assert.AreEqual(EndpointName.Breeze, testProvider.endpointName);

            _ = controller.LiveMetrics;
            Assert.AreEqual(EndpointName.LiveMetrics, testProvider.endpointName);

            _ = controller.Profiler;
            Assert.AreEqual(EndpointName.Profiler, testProvider.endpointName);

            _ = controller.Snapshot;
            Assert.AreEqual(EndpointName.Snapshot, testProvider.endpointName);
        }

        [TestMethod]
        public void VerifyControllerCache()
        {
            var testProvider = new TestProvider();
            var controller = new EndpointController(testProvider);

            // Test1 ) should return expected endpoint
            var test1Endpoint = "https://127.0.0.1/test1";
            testProvider.TestEndpoint = test1Endpoint;
            var test1 = controller.Breeze;
            Assert.AreEqual(test1Endpoint, test1.AbsoluteUri);

            // Test2) should return #1 endpoint. Controller should cache value from first request.
            var test2Endpoint = "https://127.0.0.1/test2";
            testProvider.TestEndpoint = test2Endpoint;
            var test2 = controller.Breeze;
            Assert.AreEqual(test1Endpoint, test2.AbsoluteUri);

            // Test3) Controller should reset cache if the Connection String is changed.
            controller.ConnectionString = "some value";
            var test3 = controller.Breeze;
            Assert.AreEqual(test2Endpoint, test3.AbsoluteUri);
        }

        private class TestProvider : IEndpointProvider
        {
            public EndpointName endpointName { get; private set; }

            public string TestEndpoint { get; set; } = "https://127.0.0.1";

            public string ConnectionString { get; set; }

            public Uri GetEndpoint(EndpointName endpointName)
            {
                this.endpointName = endpointName;

                return new Uri(this.TestEndpoint);
            }
        }
    }
}
