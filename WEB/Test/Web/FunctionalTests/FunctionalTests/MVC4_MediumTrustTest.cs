namespace FunctionalTests
{
    using Functional.Helpers;
    using Functional.IisExpress;
    using Helpers;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;

    [TestClass]
    public class Mvc4MediumTrustTest : RequestTelemetryTestBase
    {
        private const int TimeoutInMs = 10000;
        private const string ApplicationDirName = @"..\TestApps\Mvc4_MediumTrust\App";

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                ApplicationDirName);
            applicationDirectory = Path.GetFullPath(applicationDirectory);
            Trace.WriteLine("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 44918,
                    })
                {
                    TelemetryListenerPort = 4004,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                    AttachDebugger = System.Diagnostics.Debugger.IsAttached
                });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.StopWebAppHost();
        }

        [TestMethod]         
        public void Test4Medium200RequestAsync()
        {
            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            
            var responseTask = this.HttpClient.GetStringAsync("products/All");
            const string expectedRequestName = "GET products/All";
            string expectedRequestUrl = this.Config.ApplicationUri + "/products/All";

            responseTask.Wait(TimeoutInMs);
            Assert.IsTrue(responseTask.Result.StartsWith("[{"));

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
        }

        [TestMethod]
        public void TestRequestPropertiesAreCollectedForDangerousRequest()
        {
            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            const string path = "/products<br/>";
            const string expectedRequestName = "GET " + path;
            string expectedRequestUrl = this.Config.ApplicationUri + path;

            var appRequest = (HttpWebRequest)WebRequest.Create(expectedRequestUrl);
            
            try
            {
                appRequest.GetResponse();
                Assert.Fail("Task was supposed to fail with 400");
            }
            catch (WebException exp)
            {
                Trace.WriteLine(exp.Message);
            }

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];
            
            var testFinish = DateTimeOffset.UtcNow;
            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "400", false, request, testStart, testFinish);
        }

        [TestMethod]               
        public void Test4Medium200RequestSync()
        {
            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            var appRequest = (HttpWebRequest)WebRequest.Create(this.Config.ApplicationUri + "/products/All");
            const string expectedRequestName = "GET products/All";
            string expectedRequestUrl = this.Config.ApplicationUri + "/products/All";

            var result = appRequest.GetResponse().GetContent();
            Assert.IsTrue(result.StartsWith("[{"));

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
        }

        [TestMethod]        
        public void Test4Medium454RequestAsync()
        {
            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            var asyncTask = HttpClient.GetStringAsync("/products/product?id=101");
            const string expectedRequestName = "GET products/product";
            string expectedRequestUrl = this.Config.ApplicationUri + "/products/product?id=101";

            try
            {
                asyncTask.Wait(TimeoutInMs);
                Assert.Fail("Task was supposed to fail with 404");
            }
            catch (AggregateException exp)
            {
                Trace.WriteLine(exp.InnerException.Message);
            }

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "404", false, request, testStart, testFinish);
        }

        [TestMethod]
        public void Test4Medium454RequestSync()
        {
            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            var appRequest = (HttpWebRequest)WebRequest.Create(this.Config.ApplicationUri + "/products/product?id=101");
            const string expectedRequestName = "GET products/product";
            string expectedRequestUrl = this.Config.ApplicationUri + "/products/product?id=101";

            try
            {
                appRequest.GetResponse();
                Assert.Fail("Task was supposed to fail with 404");
            }
            catch (WebException exp)
            {
                Trace.WriteLine(exp.Message);
            }

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];
            
            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "404", false, request, testStart, testFinish);
        }

        [TestMethod]        
        public void TestDiagnosticsFW45()
        {
            var request = (HttpWebRequest)WebRequest.Create(
                this.Config.ApplicationUri + "/products/All");

            var result = request.GetResponse().GetContent();
            Assert.IsTrue(result.StartsWith("[{"));

            var items = Listener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<MessageData>>(TimeoutInMs);

            Assert.IsTrue(items.Length > 0, "Trace items were not recieved");
        }
    }
}
