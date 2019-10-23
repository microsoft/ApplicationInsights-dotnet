namespace Functional
{
    using Helpers;
    using IisExpress;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Test to evaluate if WCF 4.0 works when transferRequestHandler is associated with the first request and a null handler is associated with
    /// the inner second request.
    /// </summary>
    [TestClass]
    public class Wcf45TransferHandlerTests : SingleWebHostTestBase
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\Wcf45Tests\App";
        private const string TestWebApplicaionDestPath = @"..\TestApps\Wcf45Tests\App";

        private const int TestListenerTimeoutInMs = 5000;

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    TestWebApplicaionDestPath);

            applicationDirectory = Path.GetFullPath(applicationDirectory);
            Trace.TraceInformation("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 31337,
                    })
                {
                    TelemetryListenerPort = 4006,
                    AttachDebugger = Debugger.IsAttached
                });
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            this.StopWebAppHost();
        }

        /// <summary>
        /// Tests if we return the telemetry object associated with the inner request (occurs specifically in wcf framework)
        /// where the handler is null. We ignore the outer request where the handler is transferRequestHandler.
        /// </summary>        
        [TestMethod]        
        public void TestTelemetryObjectCountWhenTransferRequestHandlerIsUsedInWcf()
        {
            const string requestPath = "/WcfEndpoint.svc/GetMethodTrue";

            var responseTaskResult = HttpClient.GetAsync(requestPath).Result;

            var responseData = responseTaskResult.Content.ReadAsStringAsync().Result;
            Trace.Write(responseData);

            Assert.IsTrue(
                responseTaskResult.IsSuccessStatusCode,
                "Request failed");

            var items = Listener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RequestData>>(TestListenerTimeoutInMs);

            Assert.AreEqual(1, items.Length, "Unexpected amount of requests received");
        }
    }
}
