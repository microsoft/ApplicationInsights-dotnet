// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestsRequestTelemetryFW45AspxIntegratedWebSecurityOn.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional
{
    using System.Diagnostics;
    using System.IO;
    using System.Net;

    using Helpers;
    using IisExpress;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestsRequestTelemetryFW45AspxIntegratedWebSecurityOn : SingleWebHostTestBase
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\Wa45Aspx\App";
        private const string TestWebApplicaionDestPath = "TestApps_TestsRequestTelemetryFW45AspxIntegratedWebSecurityOn_App";

        private const int TestRequestTimeoutInMs = 150000;

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    TestWebApplicaionDestPath);

            Trace.WriteLine("Application directory:" + applicationDirectory);

            File.Copy(
                Path.Combine(applicationDirectory, "App_Data", "Integrated.WebSecurityOn.Web.config"),
                Path.Combine(applicationDirectory, "Web.config"),
                true);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 31337,
                    })
                {
                    TelemetryListenerPort = 4005,
                    AttachDebugger = Debugger.IsAttached
                });
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            this.StopWebAppHost();
        }

        /// <summary>
        /// Tests request telemetry collecting when [authorization/deny] section is specified
        /// </summary>
        [Owner("sergeyni")]
        [Description("Tests request telemetry collecting when [deny] section is specified")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        [TestMethod]
        public void TestCustomSecuirtyDenyAllRequestCollecting()
        {
            const string RequestPath = "/SyncWebForm.aspx";
            const string ContentMarker =
                "<title>Access is denied.</title>";

            var responseTask = this.HttpClient.GetAsync(RequestPath);

            Assert.IsTrue(
                responseTask.Wait(TestRequestTimeoutInMs),
                "Request was not executed in time");

            Assert.IsFalse(
                responseTask.Result.IsSuccessStatusCode,
                "Request succeeded");

            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                responseTask.Result.StatusCode,
                "Unexpected response code");

            var responseData = responseTask.Result.Content.ReadAsStringAsync().Result;
            Trace.Write(responseData);

            Assert.IsTrue(
                responseData.Contains(ContentMarker),
                "Exception description does not contain expected data: {0}",
                responseData);

            //// Validating telemetry results
            const int TimeToListenToEvents = 15000;
            var items = Listener.ReceiveAllItemsDuringTime(TimeToListenToEvents);

            Assert.AreEqual(1, items.Length, "Unexpected count of events received: expected 1 request");
        }
    }
}