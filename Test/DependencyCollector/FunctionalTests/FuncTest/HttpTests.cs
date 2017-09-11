namespace FuncTest
{
    using System;    
    using System.Linq;
    using AI;
    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;
    using FuncTest.IIS;

    /// <summary>
    /// Tests Dependency Collector (HTTP) Functionality for a WebApplication written in classic ASP.NET
    /// </summary>
    [TestClass]
    public class HttpTests
    {
        /// <summary>
        /// Folder for ASPX 4.5.1 test application deployment.
        /// </summary>        
        internal const string Aspx451AppFolder = ".\\Aspx451";

        /// <summary>
        /// Folder for ASPX 4.5.1 Win32 mode test application deployment.
        /// </summary>        
        internal const string Aspx451AppFolderWin32 = ".\\Aspx451Win32";

        internal static IISTestWebApplication Aspx451TestWebApplication { get; private set; }

        internal static IISTestWebApplication Aspx451TestWebApplicationWin32 { get; private set; }
        

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            Aspx451TestWebApplication = new IISTestWebApplication
            {
                AppName = "Aspx451",
                Port = DeploymentAndValidationTools.Aspx451Port,
            };

            Aspx451TestWebApplicationWin32 = new IISTestWebApplication
            {
                AppName = "Aspx451Win32",
                Port = DeploymentAndValidationTools.Aspx451PortWin32,
                EnableWin32Mode = true,
            };

            DeploymentAndValidationTools.Initialize();

            AzureStorageHelper.Initialize();

            Aspx451TestWebApplication.Deploy();
            Aspx451TestWebApplicationWin32.Deploy();

            Trace.TraceInformation("IIS Restart begin.");
            Iis.Reset();
            Trace.TraceInformation("IIS Restart end.");
            

            Trace.TraceInformation("HttpTests class initialized");
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            AzureStorageHelper.Cleanup();            
            DeploymentAndValidationTools.CleanUp();
            Aspx451TestWebApplication.Remove();
            Aspx451TestWebApplicationWin32.Remove();
            Trace.TraceInformation("IIS Restart begin.");
            Iis.Reset();
            Trace.TraceInformation("IIS Restart end.");
            Trace.TraceInformation("HttpTests class cleaned up");

        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            DeploymentAndValidationTools.SdkEventListener.Start();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {            
            DeploymentAndValidationTools.SdkEventListener.Stop();
        }
        
        private const string Aspx451TestAppFolder = "..\\TestApps\\ASPX451\\App\\";

        private static void EnsureNet451Installed()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
        }

        #region Tests

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForSyncHttpAspx451()
        {
            EnsureNet451Installed();

            // Execute and verify calls which succeeds            
            HttpTestHelper.ExecuteSyncHttpTests(Aspx451TestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, "200", HttpTestConstants.QueryStringOutboundHttp);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForSyncHttpPostCallAspx451()
        {
            EnsureNet451Installed();

            // Execute and verify calls which succeeds            
            HttpTestHelper.ExecuteSyncHttpPostTests(Aspx451TestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, "200", HttpTestConstants.QueryStringOutboundHttpPost);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForSyncHttpFailedAspx451()
        {
            EnsureNet451Installed();

            // Execute and verify calls which fails.            
            HttpTestHelper.ExecuteSyncHttpTests(Aspx451TestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpInitial, "404", HttpTestConstants.QueryStringOutboundHttpFailed);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAsync1HttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncTests(Aspx451TestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundHttpAsync1, "200");
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForHttpAspx451WithHttpClient()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteSyncHttpClientTests(Aspx451TestWebApplication, HttpTestConstants.AccessTimeMaxHttpNormal, "404");
        }

        [TestMethod]
        [Description("Verify RDD is collected for failed Async Http Calls in ASPX 4.5.1 application")]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForFailedAsync1HttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncTests(Aspx451TestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpInitial, HttpTestConstants.QueryStringOutboundHttpAsync1Failed, "404");
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAsync2HttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncTests(Aspx451TestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundHttpAsync2, "200");
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForFailedAsync2HttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncTests(Aspx451TestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpInitial, HttpTestConstants.QueryStringOutboundHttpAsync2Failed, "404");
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAsync3HttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncTests(Aspx451TestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, HttpTestConstants.QueryStringOutboundHttpAsync3, "200");
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForFailedAsync3HttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncTests(Aspx451TestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpInitial, HttpTestConstants.QueryStringOutboundHttpAsync3Failed, "404");
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAsyncWithCallBackHttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncWithCallbackTests(Aspx451TestWebApplication, true, HttpTestConstants.AccessTimeMaxHttpInitial, "200", HttpTestConstants.QueryStringOutboundHttpAsync4);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAsyncAwaitHttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncAwaitTests(Aspx451TestWebApplication, true, HttpTestConstants.AccessTimeMaxHttpInitial, "200", HttpTestConstants.QueryStringOutboundHttpAsyncAwait1);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForFailedAsyncAwaitHttpAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAsyncAwaitTests(Aspx451TestWebApplication, false, HttpTestConstants.AccessTimeMaxHttpInitial, "404", HttpTestConstants.QueryStringOutboundHttpAsyncAwait1Failed);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAzureSdkBlobAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAzureSDKTests(Aspx451TestWebApplication, 1, "blob", "http://127.0.0.1:11000", HttpTestConstants.QueryStringOutboundAzureSdk, true);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAzureSdkQueueAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAzureSDKTests(Aspx451TestWebApplication, 1, "queue", "http://127.0.0.1:11001", HttpTestConstants.QueryStringOutboundAzureSdk, false);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestRddForAzureSdkTableAspx451()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteAzureSDKTests(Aspx451TestWebApplication, 1, "table", "http://127.0.0.1:11002", HttpTestConstants.QueryStringOutboundAzureSdk, true);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolderWin32)]
        public void TestRddForWin32ApplicationPool()
        {
            EnsureNet451Installed();

            HttpTestHelper.ExecuteSyncHttpTests(Aspx451TestWebApplicationWin32, true, 1, HttpTestConstants.AccessTimeMaxHttpInitial, "200", HttpTestConstants.QueryStringOutboundHttp);
        }

        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [Description("Validates that DependencyModule collects telemety for outbound connections to non existent hosts. This request is expected to fail at DNS resolution stage, and hence will not contain http code in result.")]
        [DeploymentItem(Aspx451TestAppFolder, Aspx451AppFolder)]
        public void TestDependencyCollectionForFailedRequestAtDnsResolution()
        {
            EnsureNet451Installed();

            var queryString = HttpTestConstants.QueryStringOutboundHttpFailedAtDns;
            var resourceNameExpected = HttpTestHelper.ResourceNameHttpToFailedAtDnsRequest;
            Aspx451TestWebApplication.ExecuteAnonymousRequest(queryString);

            //// The above request would have trigged RDD module to monitor and create RDD telemetry
            //// Listen in the fake endpoint and see if the RDDTelemtry is captured
            var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
            var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();

            Assert.AreEqual(
                1,
                httpItems.Length,
                "Total Count of Remote Dependency items for HTTP collected is wrong.");

            var httpItem = httpItems[0];

            // Since the outbound request would fail at DNS resolution, there won't be any http code to collect.
            // This will be a case where success = false, but resultCode is empty            
            Assert.AreEqual(false, httpItem.data.baseData.success, "Success flag collected is wrong.");

            // Result code is collected only in profiler case.
            var expectedResultCode = DeploymentAndValidationTools.ExpectedHttpSDKPrefix == "rddp" ? "NameResolutionFailure" : string.Empty;
            Assert.AreEqual(expectedResultCode, httpItem.data.baseData.resultCode, "Result code collected is wrong.");
            string actualSdkVersion = httpItem.tags[new ContextTagKeys().InternalSdkVersion];
            Assert.IsTrue(actualSdkVersion.Contains(DeploymentAndValidationTools.ExpectedHttpSDKPrefix), "Actual version:" + actualSdkVersion);
        }

        #endregion 451        
    }
}