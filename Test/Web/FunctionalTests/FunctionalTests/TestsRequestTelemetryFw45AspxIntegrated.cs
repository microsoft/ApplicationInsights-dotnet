// -----------------------------------------------------------------------
// <copyright file="TestsRequestTelemetryFW45AspxIntegrated.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// All rights reserved.  2014
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary></summary>
// -----------------------------------------------------------------------

namespace Functional
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Helpers;
    using IisExpress;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestsRequestTelemetryFW45AspxIntegrated : RequestTelemetryTestBase
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\Wa45Aspx\App";
        private const string TestWebApplicaionDestPath = "TestApps_TestsRequestTelemetryFW45AspxIntegrated_App";

        private const int TestRequestTimeoutInMs = 150000;
        private const int TestListenerTimeoutInMs = 5000;

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    TestWebApplicaionDestPath);

            Trace.WriteLine("Application directory:" + applicationDirectory);

            File.Copy(
                Path.Combine(applicationDirectory, "App_Data", "IntegratedPipeline.Web.config"),
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
                    AttachDebugger = Debugger.IsAttached,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
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
        [Owner("sergeyni")]
        [Description("Tests correct values of StartTime and duration in collected request telemetry")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        [TestMethod]
        public void TestFW45Aspx()
        {
            const string RequestPath = "/SyncWebForm.aspx";
            const string expectedRequestName = "GET " + RequestPath;
            string expectedRequestUrl = this.Config.ApplicationUri + RequestPath;

            const string ContentMarker =
                "<title>SyncWebForm.aspx</title>";

            DateTimeOffset testStart = DateTimeOffset.UtcNow;

            var responseTask = this.HttpClient.GetAsync(RequestPath);

            Assert.IsTrue(
                responseTask.Wait(TestRequestTimeoutInMs),
                "Request was not executed in time");

            Assert.IsTrue(
                responseTask.Result.IsSuccessStatusCode,
                "Request failed");

            var responseData = responseTask.Result.Content.ReadAsStringAsync().Result;
            var testFinish = DateTimeOffset.UtcNow;

            Trace.TraceInformation(responseData);

            Assert.IsTrue(
                responseData.Contains(ContentMarker),
                "Exception description does not contain expected data: {0}",
                responseData);

            var request = Listener.ReceiveItemsOfType<TelemetryItem<RequestData>>(1, TestListenerTimeoutInMs)[0];

            this.TestWebApplicationHelper(expectedRequestName, expectedRequestUrl, "200", true, request, testStart, testFinish);
        }
    }
}