// -----------------------------------------------------------------------
// <copyright file="TestsRequestTelemetryFW45AspxIntegratedSendTelemetryOnStart.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// All rights reserved.  2014
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary></summary>
// -----------------------------------------------------------------------

namespace Functional
{
    using System.Linq;
    using Helpers;
    using IisExpress;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    [TestClass]
    public class TestsRequestTelemetryFW45AspxIntegratedSendTelemetryOnStart : SingleWebHostTestBase
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\WebAppFW45\App";
        private const string TestWebApplicaionDestPath = @"..\TestApps\WebAppFW45\App";

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

            UpdateAppConfigSettings(
                new Dictionary<string, string>
                {
                    { "TestApp.SendTelemetryItemOnAppStart", "true" }
                },
                Path.Combine(applicationDirectory, "Web.config"));

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 31337,
                    })
                {
                    TelemetryListenerPort = 4017,
                    AttachDebugger = Debugger.IsAttached
                });
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            this.StopWebAppHost();
        }

        /// <summary>
        /// Tests correct values of StartTime and duration in collected request telemetry
        /// </summary>        
        [Description("Tests correct values of StartTime and duration in collected request telemetry")]        
        [TestMethod]
        public void TestSendingTelemetryOnWebApplicationStart()
        {
            const string RequestPath = "/Aspx/TestWebForm.aspx";
            const string ContentMarker = "TestWebForm.aspx";
            const string ExpectedTelemetryMessage = "Application_Start";

            var responseTask = this.HttpClient.GetAsync(RequestPath);

            Assert.IsTrue(
                responseTask.Wait(TestRequestTimeoutInMs),
                "Request was not executed in time");

            Assert.IsTrue(
                responseTask.Result.IsSuccessStatusCode,
                "Request failed");

            var responseData = responseTask.Result.Content.ReadAsStringAsync().Result;
            Trace.TraceInformation(responseData);

            Assert.IsTrue(
                responseData.Contains(ContentMarker),
                "Exception description does not contain expected data: {0}",
                responseData);

            var traceItems = this.Listener
                .ReceiveAllItemsDuringTimeOfType<TelemetryItem<MessageData>>(TestListenerTimeoutInMs)
                .Where(item => item.data.baseData.message.Contains(ExpectedTelemetryMessage))
                .ToList();
            
            Assert.AreEqual(
                1,
                traceItems.Count, "Unexpected count of received items");

            Assert.AreEqual(ExpectedTelemetryMessage, traceItems[0].data.baseData.message, "Message is not expected");
        }
    }
}