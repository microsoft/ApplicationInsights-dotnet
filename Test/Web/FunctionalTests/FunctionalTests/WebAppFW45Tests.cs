namespace FunctionalTests
{
    using System.Net;
    using Functional.Asmx;
    using Functional.Helpers;
    using Functional.IisExpress;  
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;

    [TestClass]
    public class WebAppFw45Tests : RequestTelemetryTestBase
    {
        private const int TimeoutInMs = 15000;
        private const int TestListenerWaitTimeInMs = 40000;

        private const string TestWebApplicaionSourcePath = @"..\TestApps\WebAppFW45\App";
        private const string TestWebApplicaionDestPath = @"TestsUserSessionFW45Wcf45";

        private static TestWebServiceSoapClient asmxClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicaionDestPath);

            Trace.WriteLine("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 4321,
                    })
                {
                    TelemetryListenerPort = 4017,
                    AttachDebugger = Debugger.IsAttached,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.StopWebAppHost();
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            asmxClient = new TestWebServiceSoapClient();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            asmxClient.Close();
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void Mvc200RequestFW45BasicRequestValidationAndHeaders()
        {
            const string requestPath = "api/products";
            const string expectedRequestName = "GET products";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            //Call an applicaiton page
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage {
                RequestUri = new Uri(expectedRequestUrl),
                Method = HttpMethod.Get,
            };
            
            requestMessage.Headers.Add("x-forwarded-for", "1.2.3.4:54321");

            var responseTask = client.SendAsync(requestMessage);
            responseTask.Wait(TimeoutInMs);
            var responseTextTask = responseTask.Result.Content.ReadAsStringAsync();
            responseTextTask.Wait(TimeoutInMs);
            Assert.IsTrue(responseTextTask.Result.StartsWith("[{"));

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
          
            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl , "200", true, request, testStart, testFinish);
            Assert.AreEqual("1.2.3.4", request.tags[new ContextTagKeys().LocationIp]);
        }

        [TestMethod]
        [Owner("mafletch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void Mvc200RequestFW45BasicRequestSyntheticFiltering()
        {
            const string requestPath = "api/products";
            const string expectedRequestName = "GET products";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            //Call an applicaiton page
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(expectedRequestUrl),
                Method = HttpMethod.Get,
            };

            requestMessage.Headers.Add("User-Agent", "bingbot");

            var responseTask = client.SendAsync(requestMessage);
            responseTask.Wait(TimeoutInMs);
            var responseTextTask = responseTask.Result.Content.ReadAsStringAsync();
            responseTextTask.Wait(TimeoutInMs);
            Assert.IsTrue(responseTextTask.Result.StartsWith("[{"));

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
            Assert.AreEqual("Bot", request.tags[new ContextTagKeys().OperationSyntheticSource]);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestMvc404Request()
        {
            const string requestPath = "api/products/101";
            const string expectedRequestName = "GET products [id]";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + "api/products/101";

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            var asyncTask = HttpClient.GetStringAsync(requestPath);

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
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestWcf200OneWayRequest()
        {
            const string requestPath = "Wcf/WcfEndpoint.svc/OneWayMethod";
            const string expectedRequestName = "POST /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            var postTask = HttpClient.PostAsync(requestPath, null);
            postTask.Wait(TimeoutInMs);
            Assert.IsTrue(postTask.Result.IsSuccessStatusCode);

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "202", true, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestWcf200PostRequest()
        {
            const string requestPath = "Wcf/WcfEndpoint.svc/PostMethod";
            const string expectedRequestName = "POST /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            var postTask = HttpClient.PostAsync(requestPath, null);
            postTask.Wait(TimeoutInMs);
            Assert.IsTrue(postTask.Result.IsSuccessStatusCode);

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];
            
            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("sergeyni")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestWcf200GetRequest()
        {
            const string requestPath = "Wcf/WcfEndpoint.svc/GetMethod/fail/false";
            const string expectedRequestName = "GET /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            var getTask = HttpClient.GetAsync(requestPath);
            getTask.Wait(TimeoutInMs);
            Assert.IsTrue(getTask.Result.IsSuccessStatusCode);

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            DateTimeOffset testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestWcf503PostRequest()
        {
            const string requestPath = "Wcf/WcfEndpoint.svc/PostMethod";
            const string expectedRequestName = "POST /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            try
            {
                var postTask = HttpClient.PostAsync(requestPath, CreateBooleanContent(true));
                postTask.Wait(TimeoutInMs);
                Assert.IsFalse(postTask.Result.IsSuccessStatusCode);
            }
            catch (Exception exp)
            {
                Trace.WriteLine(exp.Message);
            }

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "503", false, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("sergeyni")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestWcf503GetRequest()
        {
            const string requestPath = "Wcf/WcfEndpoint.svc/GetMethod/fail/true";
            const string expectedRequestName = "GET /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            try
            {
                var postTask = HttpClient.GetAsync(requestPath);
                postTask.Wait(TimeoutInMs);
                Assert.IsFalse(postTask.Result.IsSuccessStatusCode);
            }
            catch (Exception exp)
            {
                Trace.WriteLine(exp.Message);
            }

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "503", false, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestAsmx200Request()
        {
            const string requestPath = "Asmx/TestWebService.asmx";
            const string expectedRequestName = "POST /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);
              
            string output = asmxClient.HelloWorld();
            if (string.IsNullOrEmpty(output))
            {
                Assert.Fail("Test application is broken.");
            }

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestAsmx_500Request()
        {
            const string requestPath = "Asmx/TestWebService.asmx";
            const string expectedRequestName = "POST /" + requestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;
            Trace.WriteLine("Start: " + testStart);

            bool failed;
            try
            {
                asmxClient.HelloPost(true);
                failed = false;
            }
            catch (Exception exp)
            {
                failed = true;
                Trace.WriteLine(exp.Message);
            }

            Assert.IsTrue(failed, "Request was supposed to fail.");

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
            Trace.WriteLine("Finish: " + testFinish);

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "500", false, request, testStart, testFinish);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestAsmx_CheckExceptionAndRequestCollectedForResourceNotFound()
        {
            var postTask = HttpClient.GetAsync("asmx/WcfEndpointBad.svc/GetMethod");
            postTask.Wait(TimeoutInMs);
            
            Assert.AreEqual(HttpStatusCode.NotFound, postTask.Result.StatusCode, "Request failed with incorrect status code");

            var items = Listener.ReceiveItemsOfTypes <TelemetryItem<RequestData>, TelemetryItem<ExceptionData>>(2, TimeoutInMs);

            // One item is request, the other one is exception.
            int requestItemIndex = (items[0] is TelemetryItem<RequestData>) ? 0 : 1;
            int exceptionItemIndex = (requestItemIndex == 0) ? 1 : 0;

            Assert.AreEqual(this.Config.IKey, items[requestItemIndex].iKey, "IKey is not the same as in config file");
            Assert.AreEqual(this.Config.IKey, items[exceptionItemIndex].iKey, "IKey is not the same as in config file");

            // Check that request id is set in exception operation parentId
            Assert.AreEqual(
                ((TelemetryItem<RequestData>)items[requestItemIndex]).data.baseData.id,
                items[exceptionItemIndex].tags[new ContextTagKeys().OperationParentId],
                "Exception ParentId is not same as Request id");

            // Check that request and exception have the same operation id
            Assert.AreEqual(
                items[requestItemIndex].tags[new ContextTagKeys().OperationId],
                items[exceptionItemIndex].tags[new ContextTagKeys().OperationId],
                "Exception Operation Id is not same as Request Operation Id");
        }

        /// <summary>
        /// Tests a special scenario in the case of WCF 4.5 application where an internal request is created for every request issued.
        /// However, we filter the inner request based on the type of handler used. The outer request (the one that we are not interested in)
        /// is associated with transferRequestHandler, whereas the inner request is associated with null handler. This test verifies if we
        /// are returning 1 telemetry object or not (where we have 2 requests).
        /// </summary>
        [Owner("sergeyni")]
        [TestMethod]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestTelemetryObjectCountWhenTransferRequestHandlerIsUsedInWcf()
        {
            HttpClient.GetAsync("Wcf/WcfEndpoint.svc/GetMethodTrue");

            var items = 
                Listener
                    .ReceiveAllItemsDuringTimeOfType<TelemetryItem<RequestData>>(TestListenerWaitTimeInMs);

            Assert.AreEqual(1, items.Count(), "Unexpected number of requests received");
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestApplicationVersionIsPopulatedFromBuildInfoFile()
        {
            HttpClient.GetAsync("Wcf/WcfEndpoint.svc/GetMethodTrue");

            var items =
                Listener
                    .ReceiveAllItemsDuringTime(TestListenerWaitTimeInMs).ToArray();

            foreach (var item in items)
            {
                // Build version locally starts with AutoGen but on rolling build it starts with branch name and has version 
                Assert.IsTrue(
                    item.tags[new ContextTagKeys().ApplicationVersion].Length > 0,
                    "Application version was not populated from the buildInfo file. Current: " + item.tags[new ContextTagKeys().ApplicationVersion]);
            }
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
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

        /// <summary>
        /// Creates content to send with HttpClient
        /// Content type is: application/xml
        /// Content value is <boolean xmlns="http://schemas.microsoft.com/2003/10/Serialization/\">Value</boolean>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private HttpContent CreateBooleanContent(bool value)
        {
            return new StringContent(
                string.Format("<boolean xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</boolean>",
                    value.ToString().ToLower(CultureInfo.InvariantCulture)),
                Encoding.UTF8,
                "application/xml");
        }
    }
}