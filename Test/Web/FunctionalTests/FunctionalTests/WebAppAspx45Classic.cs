// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestsWebAppFW45Aspx.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary>
// Functional tests for WebAppFw45 hosted web pages
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using Functional.Helpers;
    using IisExpress;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Functional tests for hosted web pages
    /// </summary>
    [TestClass]
    public class WebAppAspx45Classic : RequestTelemetryTestBase
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\Aspx45\App";
        private const string TestWebApplicaionDestPath = "TestApps_Aspx45_App";

        private const int TestRequestTimeoutInMs = 15000;
        private const int TestListenerTimeoutInMs = 5000;

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
                        ApplicationPool = IisExpressAppPools.Clr4ClassicAppPool,
                        Path = applicationDirectory,
                        Port = 31337,
                    })
                {
                    TelemetryListenerPort = 4001,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                    AttachDebugger = System.Diagnostics.Debugger.IsAttached,
                });
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            this.StopWebAppHost();
        }

        /// <summary>
        /// Tests 200 OK HTTP status code request execution and collecting result 
        /// </summary>
        [TestMethod]
        [Owner("sergeyni")]
        [Description("Tests 200 OK HTTP status code request execution and collecting result ")]
        [Ignore] //See: https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/623
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TestAspx200StatusCodeOnRequest()
        {
            const string RequestPath = "/TestWebForm.aspx";
            const string expectedRequestName = "GET " + RequestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + RequestPath;

            const string ResponseValidContentSignature = "TestWebForm.aspx";

            var requestStartTime = DateTimeOffset.UtcNow;

            this.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyAgent/1.0");
            var responseTask = this.HttpClient.GetAsync(RequestPath);

            Assert.IsTrue(
                responseTask.Wait(TestRequestTimeoutInMs),
                "Request was not executed in time");

            var responseData = responseTask.Result.Content.ReadAsStringAsync().Result;
            Trace.Write(responseData);

            Assert.IsTrue(
                responseTask.Result.IsSuccessStatusCode,
                "Request failed");

            var requestEndTime = DateTimeOffset.UtcNow;

            Assert.IsTrue(
                responseData.Contains(ResponseValidContentSignature),
                "Unexpected response content");

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TestListenerTimeoutInMs)[0];

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, requestStartTime, requestEndTime);
        }

        [TestMethod]
        [Owner("sergeyni")]
        [Description("Tests 500 HTTP status code request execution and collecting result ")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        [Ignore] //See: https://github.com/Microsoft/ApplicationInsights-dotnet-server/issues/623
        public void TestAspx500StatusCodeOnRequest()
        {
            const string RequestPath = "/TestWebForm.aspx";
            const string RequestPathWithArguments = RequestPath + "?ifThrow=true";
            const string expectedRequestName = "GET " + RequestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + RequestPathWithArguments;

            var requestStartTime = DateTimeOffset.UtcNow;

            var responseTask = this.HttpClient.GetStringAsync(RequestPathWithArguments);

            try
            {
                Assert.IsTrue(
                    responseTask.Wait(TestRequestTimeoutInMs),
                    "Request was not executed in time");

                Assert.Fail("An exception must be thrown");
            }
            catch (AggregateException exc)
            {
                Assert.IsInstanceOfType(
                    exc.InnerException, 
                    typeof(HttpRequestException), 
                    "Unexpected exception type was thrown from http client");

                var inner = (HttpRequestException)exc.InnerException;

                Assert.IsTrue(
                    inner.Message.Contains(" 500 "), 
                    "Exception description does not contain expected data: {0}", 
                    inner.Message);
            }

            var requestEndTime = DateTimeOffset.UtcNow;

            var request = Listener.ReceiveItemsOfType <TelemetryItem<RequestData>>(1, TestListenerTimeoutInMs)[0];

            // TODO: Fix status code detection for classic mode. Now status code is detected incorrectly.
            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, requestStartTime, requestEndTime);
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        [Ignore]
        // This case works fine for Integrated pipeline mode, and module is not called at all for classic mode
        public void TestAspx_CollectRequestAndExceptionForResourceNotFound()
        {
            const string RequestPath = "/WrongWebForm.aspx";

            var responseTask = this.HttpClient.GetStringAsync(RequestPath);

            bool failed;
            try
            {
                Assert.IsTrue(responseTask.Wait(TestRequestTimeoutInMs), "Request was not executed in time");
                failed = false;
            }
            catch (AggregateException exp)
            {
                failed = true;
                Trace.WriteLine(exp.InnerException.Message);
            }

            Assert.IsTrue(failed, "Request was supposded to fail");

            var items = Listener.ReceiveItems(2, TestListenerTimeoutInMs);

            // One item is request, the other one is exception.
            int requestItemIndex = (items[0] is TelemetryItem<RequestData>) ? 0 : 1;
            int exceptionItemIndex = (requestItemIndex == 0) ? 1 : 0;

            Assert.AreEqual(this.Config.IKey, items[requestItemIndex].iKey, "IKey is not the same as in config file");
            Assert.AreEqual(this.Config.IKey, items[exceptionItemIndex].iKey, "IKey is not the same as in config file");

            // Check that request id is set in exception operation Id
            Assert.AreEqual(
                ((TelemetryItem<RequestData>)items[requestItemIndex]).tags[new ContextTagKeys().OperationId],
                items[exceptionItemIndex].tags[new ContextTagKeys().OperationId],
                "Operation Id is not same as Request id");  
        }

    }
}
