namespace Functional
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using Helpers;
    using IisExpress;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestsRequestTelemetryHttpModuleConfig : RequestTelemetryTestBase
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\Wa45Aspx\App";
        private const string TestWebApplicaionDestPath = @"..\TestApps\Wa45Aspx\App";

        private const int TestRequestTimeoutInMs = 150000;
        private const int TestListenerTimeoutInMs = 5000;

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicaionDestPath);
            applicationDirectory = Path.GetFullPath(applicationDirectory);
            Trace.WriteLine("Application directory:" + applicationDirectory);

            File.Copy(
                Path.Combine(applicationDirectory, "App_Data", "HttpModuleWeb.config"),
                Path.Combine(applicationDirectory, "Web.config"),
                true);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 31227,
                    })
                {
                    TelemetryListenerPort = 4005,
                    AttachDebugger = Debugger.IsAttached,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                });
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            this.StopWebAppHost();
        }
        
        [TestMethod]
        public void TestRequestPropertiesIfOnlyEndRequestWasCalled()
        {
            const string RequestPath = "/SyncWebForm.aspx";
            const string expectedRequestName = "GET " + RequestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + RequestPath;
            var testStart = DateTimeOffset.UtcNow;

            var responseTask = this.HttpClient.GetStringAsync(RequestPath);

            try
            {
                Assert.IsTrue(responseTask.Wait(TestRequestTimeoutInMs), "Request was not executed in time");
                Assert.Fail("An exception must be thrown");
            }
            catch (AggregateException exc)
            {
                Assert.IsInstanceOfType(
                    exc.InnerException,
                    typeof(HttpRequestException),
                    "Unexpected exception type was thrown from http client");

                var inner = (HttpRequestException)exc.InnerException;

                Assert.AreEqual(
                    inner.Message,
                    "Response status code does not indicate success: 401 (Unauthorized).",
                    "Request to page failed with unexpected status code");
            }

            var testFinish = DateTimeOffset.UtcNow;

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TestListenerTimeoutInMs)[0];
            
            // Duration will be 0 till we make it optional
            this.TestWebApplicationHelper(
                expectedRequestName, 
                expectedRequestUrl, 
                "401",
                true, // 401 is considred success Bug #439318 
                request, 
                testStart,
                testFinish);
        }
    }
}