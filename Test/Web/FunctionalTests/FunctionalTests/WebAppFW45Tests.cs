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
        private const string TestWebApplicaionDestPath = @"..\TestApps\WebAppFW45\App";

        private static TestWebServiceSoapClient asmxClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicaionDestPath);

            applicationDirectory = Path.GetFullPath(applicationDirectory);
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
        public void WebApi200RequestFW45BasicRequestValidationAndW3CHeaders()
        {
            const string requestPath = "api/products";
            const string expectedRequestName = "GET products";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            //Call an application page
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage {
                RequestUri = new Uri(expectedRequestUrl),
                Method = HttpMethod.Get,
            };
            
            requestMessage.Headers.Add("x-forwarded-for", "1.2.3.4:54321");
            requestMessage.Headers.Add("traceparent", "00-9d2341f8070895468dbdffb599cf49fc-0895468dbdffb519-00");
            requestMessage.Headers.Add("tracestate", "some=state");
            requestMessage.Headers.Add("Correlation-Context", "k1=v1,k2=v2");

            var responseTask = client.SendAsync(requestMessage);
            responseTask.Wait(TimeoutInMs);
            var responseTextTask = responseTask.Result.Content.ReadAsStringAsync();
            responseTextTask.Wait(TimeoutInMs);
            Assert.IsTrue(responseTextTask.Result.StartsWith("[{"));

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            var testFinish = DateTimeOffset.UtcNow;
          
            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl , "200", true, request, testStart, testFinish);
            Assert.AreEqual("1.2.3.4", request.tags[new ContextTagKeys().LocationIp]);

            // Check that request has operation Id, parentId and Id are set from headers
            Assert.AreEqual("9d2341f8070895468dbdffb599cf49fc", request.tags[new ContextTagKeys().OperationId], "Request Operation Id is not parsed from header");
            Assert.AreEqual("|9d2341f8070895468dbdffb599cf49fc.0895468dbdffb519.", request.tags[new ContextTagKeys().OperationParentId], "Request Parent Id is not parsed from header");
            Assert.IsTrue(request.data.baseData.id.StartsWith("|9d2341f8070895468dbdffb599cf49fc."), "Request Id is not properly set");

            Assert.IsTrue(request.data.baseData.properties.TryGetValue("tracestate", out var tracestate));
            Assert.AreEqual("some=state", tracestate);
            Assert.IsTrue(request.data.baseData.properties.TryGetValue("k1", out var v1));
            Assert.AreEqual("v1", v1);
        }

        [TestMethod]
        public void WebApi500RequestFW45ExceptionTracking()
        {
            const string requestPath = "api/products/5";
            const string expectedRequestName = "POST products [id]";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            //Call an applicaiton page
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(expectedRequestUrl),
                Method = HttpMethod.Post,
            };

            var responseTask = client.SendAsync(requestMessage);
            responseTask.Wait(TimeoutInMs);

            var telemetry = Listener.ReceiveAllItemsDuringTime(TimeoutInMs);
            var requests = telemetry.OfType<TelemetryItem<RequestData>>().ToArray();
            Assert.AreEqual(1, requests.Length);

            var allExceptions = telemetry.OfType<TelemetryItem<ExceptionData>>();
            // select only test exception, and filter out those that are collected by first chance module - the module is not enabled by default
            var controllerException = allExceptions.Where(i => i.data.baseData.exceptions.FirstOrDefault()?.message == "Test exception to get 500" && i.tags[new ContextTagKeys().InternalSdkVersion].StartsWith("web"));
            Assert.AreEqual(1, controllerException.Count());

            var testFinish = DateTimeOffset.UtcNow;

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "500", false, requests.Single(), testStart, testFinish);
        }

        [TestMethod]        
        public void WebApi200RequestFW45BasicRequestValidationAndRequestIdHeader()
        {
            const string requestPath = "api/products";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            //Call an applicaiton page
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri(expectedRequestUrl),
                Method = HttpMethod.Get,
            };

            requestMessage.Headers.Add("request-id", "|guid2.guid1.");

            var responseTask = client.SendAsync(requestMessage);
            responseTask.Wait(TimeoutInMs);
            var responseTextTask = responseTask.Result.Content.ReadAsStringAsync();
            responseTextTask.Wait(TimeoutInMs);
            Assert.IsTrue(responseTextTask.Result.StartsWith("[{"));

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TimeoutInMs)[0];

            // Check that request has operation Id, parentId and Id are set from headers
            var operationId = request.tags[new ContextTagKeys().OperationId];
            Assert.IsNotNull(operationId, "Request Operation Id is not parsed from header");
            Assert.AreEqual("|guid2.guid1.", request.tags[new ContextTagKeys().OperationParentId], "Request Parent Id is not parsed from header");
            Assert.IsTrue(request.data.baseData.id.StartsWith($"|{operationId}."), "Request Id is not properly set");
            Assert.IsTrue(request.data.baseData.properties.TryGetValue("ai_legacyRootId", out var legacyRootId));
            Assert.AreEqual("guid2", legacyRootId);
        }

        [TestMethod]        
        public void WebApi200RequestFW45BasicRequestSyntheticFiltering()
        {
            const string requestPath = "api/products";
            const string expectedRequestName = "GET products";
            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            //Call an application page
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
        public void TestWebApi404Request()
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
        public void TestAsmx_CheckExceptionAndRequestCollectedForResourceNotFound()
        {
            var postTask = HttpClient.GetAsync("asmx/WcfEndpointBad.svc/GetMethod");
            postTask.Wait(TimeoutInMs);
            
            Assert.AreEqual(HttpStatusCode.NotFound, postTask.Result.StatusCode, "Request failed with incorrect status code");

            // Obtains items with web prefix only so as to eliminate first chance exceptions.
            var items = Listener.ReceiveItemsOfTypesWithWebPrefix<TelemetryItem<RequestData>, TelemetryItem<ExceptionData>>(2, TimeoutInMs);
            var requestItem = (TelemetryItem<RequestData>)items.Single(r => r is TelemetryItem<RequestData>);
            var exceptionItem = (TelemetryItem<ExceptionData>)items.Single(r => r is TelemetryItem<ExceptionData>);

            Assert.AreEqual(this.Config.IKey, requestItem.iKey, "IKey is not the same as in config file");
            Assert.AreEqual(this.Config.IKey, exceptionItem.iKey, "IKey is not the same as in config file");

            // Check that request id is set in exception operation parentId for UnhandledException
            Assert.AreEqual(
                requestItem.data.baseData.id,
                exceptionItem.tags[new ContextTagKeys().OperationParentId],
                "Exception ParentId is not same as Request id");

            // Check that request and exception from UnhandledException have the same operation id
            Assert.AreEqual(
                requestItem.tags[new ContextTagKeys().OperationId],
                exceptionItem.tags[new ContextTagKeys().OperationId],
                "Exception Operation Id for exception is not same as Request Operation Id");
        }

        /// <summary>
        /// Tests a special scenario in the case of WCF 4.5 application where an internal request is created for every request issued.
        /// However, we filter the inner request based on the type of handler used. The outer request (the one that we are not interested in)
        /// is associated with transferRequestHandler, whereas the inner request is associated with null handler. This test verifies if we
        /// are returning 1 telemetry object or not (where we have 2 requests).
        /// </summary>        
        [TestMethod]        
        public void TestTelemetryObjectCountWhenTransferRequestHandlerIsUsedInWcf()
        {
            HttpClient.GetAsync("Wcf/WcfEndpoint.svc/GetMethodTrue");

            var items = 
                Listener
                    .ReceiveAllItemsDuringTimeOfType<TelemetryItem<RequestData>>(TestListenerWaitTimeInMs);

            Assert.AreEqual(1, items.Count(), "Unexpected number of requests received");
        }

        [TestMethod]        
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