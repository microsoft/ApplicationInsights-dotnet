// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RddTests.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
// RDD Functional Test logic
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FuncTest
{
    using System;
    using System.Diagnostics;
    using System.Linq;    
    using FuncTest.Helpers;
    using FuncTest.IIS;
    using FuncTest.Serialization;
    using Microsoft.Deployment.WindowsInstaller;    
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;    
    using RemoteDependencyKind = Microsoft.Developer.Analytics.DataCollection.Model.v2.DependencyKind;
    
    /// <summary>
    /// Tests RDD Functionality for a ASP.NET WebApplication in DOTNET 4.5.1 and DOTNET 4.6
    /// ASPX451 refers to the test application throughout the functional test context.
    /// The same app is used for testsing 4.5.1 and 4.6 scenarios.
    /// </summary>
    [TestClass]
    public partial class RddTests
    {

        /// <summary>
        /// Invalid SQL query only needed here because the test web app we use to run queries will throw a 500 and we can't get back the invalid query from it.
        /// </summary>        
        private const string InvalidSqlQueryToApmDatabase = "SELECT TOP 2 * FROM apm.[Database1212121]";

        /// <summary>
        /// Clause to go on end of SQL query when running XML query - only used in the failure case.
        /// </summary>        
        private const string ForXMLClauseInFailureCase = " FOR XML AUTO";

        /// <summary>
        /// Folder for ASPX 4.5.1 test application deployment.
        /// </summary>        
        private const string Aspx451AppFolder = ".\\Aspx451";

        /// <summary>
        /// Folder for ASPX 4.5.1 Win32 mode test application deployment.
        /// </summary>        
        private const string Aspx451AppFolderWin32 = ".\\Aspx451Win32";

        /// <summary>
        /// Port number in local where test application ASPX 4.5.1 is deployed.
        /// </summary>
        private const int Aspx451Port = 789;

        /// <summary>
        /// Port number in local where test application ASPX 4.5.1 is deployed in win32 mode.
        /// </summary>
        private const int Aspx451PortWin32 = 790;

        /// <summary>
        /// Sleep time to give SDK some time to send events.
        /// </summary>
        private const int SleepTimeForSdkToSendEvents = 10 * 1000;

        /// <summary>
        /// The fake endpoint to which SDK tries to sent Events for the test app ASPX 4.5.1. This should match the one used in
        /// ApplicationInsights.config for the test app being tested.
        /// </summary>
        private const string Aspx451FakeDataPlatformEndpoint = "http://localHost:8789/";
        
        /// <summary>
        /// Query string to specify Outbound HTTP Call .
        /// </summary>
        private const string QueryStringOutboundHttp = "?type=http&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call POST.
        /// </summary>
        private const string QueryStringOutboundHttpPost = "?type=httppost&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call POST.
        /// </summary>
        private const string QueryStringOutboundHttpPostFailed = "?type=failedhttppost&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call which fails.
        /// </summary>
        private const string QueryStringOutboundHttpFailed = "?type=failedhttp&count=";

        /// <summary>
        /// Query string to specify Outbound Azure sdk Call .
        /// </summary>
        private const string QueryStringOutboundAzureSdk = "?type=azuresdk{0}&count={1}";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url
        /// <c>http://msdn.microsoft.com/en-us/library/ms228967(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync1 = "?type=httpasync1&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228967(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync1Failed = "?type=failedhttpasync1&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228962(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync2 = "?type=httpasync2&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228962(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync2Failed = "?type=failedhttpasync2&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228968(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync3 = "?type=httpasync3&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228968(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync3Failed = "?type=failedhttpasync3&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync4 = "?type=httpasync4&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsync4Failed = "?type=failedhttpasync4&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsyncAwait1 = "?type=httpasyncawait1&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        private const string QueryStringOutboundHttpAsyncAwait1Failed = "?type=failedhttpasyncawait1&count=";
        
        /// <summary>
        /// Query string to specify Outbound SQL Call. 
        /// </summary>
        private const string QueryStringOutboundSql = "?type=sql&count=";

        /// <summary>
        /// Resource Name for bing.
        /// </summary>
        private const string ResourceNameHttpToBing = "http://www.bing.com/";

        /// <summary>
        /// Resource Name for failed request.
        /// </summary>
        private const string ResourceNameHttpToFailedRequest = "http://www.zzkaodkoakdahdjghejajdnad.com/";

        /// <summary>
        /// Resource Name for dev database.
        /// </summary>
        private const string ResourceNameSQLToDevApm = @".\SQLEXPRESS | RDDTestDatabase";

        /// <summary>
        /// Maximum access time for the initial call - This includes an additional 1-2 delay introduced before the very first call by Profiler V2.
        /// </summary>        
        private readonly TimeSpan AccessTimeMaxHttpInitial = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Maximum access time for calls after initial - This does not incur perf hit of the very first call.
        /// </summary>        
        private readonly TimeSpan AccessTimeMaxHttpNormal = TimeSpan.FromSeconds(3);
        
        /// <summary>
        /// Maximum access time for calls after initial - This does not incur perf hit of the very first call.
        /// </summary>        
        private readonly TimeSpan AccessTimeMaxSqlCallToApmdbNormal = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// ASPX 4.5.1 test application.
        /// </summary>
        private static readonly TestWebApplication aspx451TestWebApplication;

        /// <summary>
        /// ASPX 4.5.1 test application in Win32.
        /// </summary>
        private static readonly TestWebApplication aspx451TestWebApplicationWin32;

        /// <summary>
        /// RDD source expected.
        /// </summary>        
        private static DependencySourceType sourceExpected = DependencySourceType.Undefined;

        /// <summary>
        /// SDK event listener for receiving events sent from the SDK.
        /// </summary>
        private static HttpListenerObservable sdkEventListener;

        /// <summary>
        /// Initializes static members of the <see cref="RddTests"/> class.
        /// </summary>
        static RddTests()
        {                       
            aspx451TestWebApplication = new TestWebApplication
            {
                AppName = "Aspx451",
                Port = Aspx451Port,
                IsRedFieldApp = false
            };

            aspx451TestWebApplicationWin32 = new TestWebApplication
            {
                AppName = "Aspx451Win32",
                Port = Aspx451PortWin32,
                IsRedFieldApp = false
            };
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Sets up application pool in IIS and installs all test applications to their corresponding pool
        /// Installs APMC if DOT NET fraemwork is below 4.6.
        /// Resets IIS
        /// Starts listener to the fake DataPlatform Endpoint
        /// </summary>
        /// <param name="testContext">The test context</param>
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            // this makes all traces have a timestamp so it's easier to troubleshoot timing issues
            // looking for the better approach...
            foreach (TraceListener listener in Trace.Listeners)
            {
                listener.TraceOutputOptions |= TraceOptions.DateTime;
            }
            sdkEventListener = new HttpListenerObservable(Aspx451FakeDataPlatformEndpoint);

            aspx451TestWebApplication.Deploy();
            aspx451TestWebApplicationWin32.Deploy(true);

            AzureStorageHelper.Initialize();

            LocalDb.CreateLocalDb("RDDTestDatabase", aspx451TestWebApplication.AppFolder + "\\TestDatabase.sql");

            if (RegistryCheck.IsNet46Installed)
            {
                // .NET 4.6 onwards, there is no need of installing agent
                sourceExpected = !RegistryCheck.IsStatusMonitorInstalled ? DependencySourceType.Aic : DependencySourceType.Apmc;
            }
            else
            {
                sourceExpected = DependencySourceType.Apmc;

                if (!RegistryCheck.IsStatusMonitorInstalled)
                {
                    Installer.SetInternalUI(InstallUIOptions.Silent);
                    string installerPath = ExecutionEnvironment.InstallerPath;
                    try
                    {
                        Installer.InstallProduct(installerPath, "ACTION=INSTALL ALLUSERS=1 MSIINSTALLPERUSER=1");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Agent installer not found. Agent is required for running tests for framework version below 4.6" + ex);
                        throw;
                    }
                }
            }

            Iis.Reset();       
        }

        /// <summary>
        /// Cleans up by removing applications and app pools. Also uninstalls APMC if running tests for DOT NET 4.5.1.
        /// Stops listener.
        /// Resets IIS
        /// </summary>
        [ClassCleanup]
        public static void MyClassCleanup()
        {
            sdkEventListener.Dispose();

            aspx451TestWebApplication.Remove();
            aspx451TestWebApplicationWin32.Remove();

            AzureStorageHelper.Cleanup();
            
            if (RegistryCheck.IsNet46Installed)
            {
                // .NET 4.6 onwards, there is no need of installing agent 
            }
            else
            {
                string installerPath = ExecutionEnvironment.InstallerPath;                
                Installer.InstallProduct(installerPath, "REMOVE=ALL");               
                Iis.Reset();
            }            
        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            sdkEventListener.Start();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            Assert.IsFalse(sdkEventListener.FailureDetected, "Failure is detected. Please read test output logs.");
            sdkEventListener.Stop();
        }

        #region 451

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Sync HTTP calls, made in a ASP.NET 4.5.1 Application
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Sync Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForSyncHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            // Execute and verify calls which succeeds            
            this.ExecuteSyncHttpTests(aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal);            
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Sync HTTP calls, made in a ASP.NET 4.5.1 Application (POST request)
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Sync Http Calls (POST) in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForSyncHttpPostCallAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            // Execute and verify calls which succeeds            
            this.ExecuteSyncHttpPostTests(aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal);
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - failed Sync HTTP calls, made in a ASP.NET 4.5.1 Application
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for failed Sync Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForSyncHttpFailedAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            // Execute and verify calls which fails.            
            this.ExecuteSyncHttpTests(aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial);            
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Async HTTP calls, made in a ASP.NET 4.5.1 Application.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Async Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAsync1HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }            
            this.ExecuteAsyncTests(aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal, QueryStringOutboundHttpAsync1);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForHttpAspx451WithHttpClient()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteSyncHttpClientTests(aspx451TestWebApplication, AccessTimeMaxHttpNormal);
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - failed Async HTTP calls, made in a ASP.NET 4.5.1 Application.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for failed Async Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForFailedAsync1HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncTests(aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial, QueryStringOutboundHttpAsync1Failed);            
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Async HTTP calls, made in a ASP.NET 4.5.1 Application.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Async Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAsync2HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
            this.ExecuteAsyncTests(aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal, QueryStringOutboundHttpAsync2);
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - failed Async HTTP calls, made in a ASP.NET 4.5.1 Application.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for failed Async Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForFailedAsync2HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncTests(aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial, QueryStringOutboundHttpAsync2Failed);
        }


        /// <summary>
        /// Tests RDD events are generated for external dependency call - Async HTTP calls, made in a ASP.NET 4.5.1 Application.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Async Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAsync3HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
            this.ExecuteAsyncTests(aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal, QueryStringOutboundHttpAsync3);
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - failed Async HTTP calls, made in a ASP.NET 4.5.1 Application.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for failed Async Http Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForFailedAsync3HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncTests(aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial, QueryStringOutboundHttpAsync3Failed);
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Async HTTP calls with Callback handlers, made in a ASP.NET 4.5.1 Application
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Async Http Calls (using call back) in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAsyncWithCallBackHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncWithCallbackTests(aspx451TestWebApplication, true);
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Async HTTP calls with async/await pattern, made in a ASP.NET 4.5.1 Application
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Async Http Calls using async-await in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAsyncAwaitHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncAwaitTests(aspx451TestWebApplication, true);
        }


        /// <summary>
        /// Tests RDD events are generated for external dependency call - failed Async HTTP calls with async/await pattern, made in a ASP.NET 4.5.1 Application
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for failed Async Http Calls using async-await in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForFailedAsyncAwaitHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncAwaitTests(aspx451TestWebApplication, false);
        }        

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Azure Blob, made in a ASP.NET 4.5 Application
        /// using Azure SDK. This is only a very basic test and does not test all aspects on Azure access.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Azure Blob sdk Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAzureSdkBlobAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAzureSDKTests(aspx451TestWebApplication, 1, "blob", "http://127.0.0.1:11000");           
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Azure Queue, made in a ASP.NET 4.5 Application
        /// using Azure SDK. This is only a very basic test and does not test all aspects on Azure access.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Azure Queue sdk Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAzureSdkQueueAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAzureSDKTests(aspx451TestWebApplication, 1, "queue", "http://127.0.0.1:11001");           
        }

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Azure Table, made in a ASP.NET 4.5 Application
        /// using Azure SDK. This is only a very basic test and does not test all aspects on Azure access.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Azure Table sdk Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForAzureSdkTableAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAzureSDKTests(aspx451TestWebApplication, 1, "table", "http://127.0.0.1:11002");           
        }

        /// <summary>
        /// Tests RDD events are generated when application pool is running win32 mode.
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for application running in Win32 Application Pool")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolderWin32)]
        public void TestRddForWin32ApplicationPool()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
            this.ExecuteSyncHttpTests(aspx451TestWebApplicationWin32, true, 1, AccessTimeMaxHttpInitial);
        }

        #endregion 451
                          
        #region helpers

        /// <summary>
        /// Helper to execute Async Http tests.
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed.</param>
        /// <param name="success">Indicates if the tests should test success or failure case.</param> 
        /// <param name="count">Number to RDD calls to be made by the test application. </param> 
        /// <param name="accessTimeMax">Approximate maximum time taken by RDD Call.  </param>
        /// <param name="url">url</param> 
        private void ExecuteAsyncTests(TestWebApplication testWebApplication, bool success, int count,
            TimeSpan accessTimeMax, string url)
        {
            var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;
            var commandNameExpected = string.Empty;

            testWebApplication.DoTest(
                application =>
                {
                    var queryString = url;
                    application.ExecuteAnonymousRequest(queryString + count);
                    application.ExecuteAnonymousRequest(queryString + count);
                    application.ExecuteAnonymousRequest(queryString + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems =
                        sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(
                            SleepTimeForSdkToSendEvents);
                    var httpItems =
                        allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        3*count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        if (DependencySourceType.Apmc == sourceExpected)
                        {
                            Assert.AreEqual("GET " + resourceNameExpected, httpItem.Data.BaseData.Name,
                                "For StatusMonitor implementation we expect verb to be collected.");
                        }

                        this.ValidateRddTelemetryValues(httpItem, resourceNameExpected, commandNameExpected, accessTimeMax, success);
                    }
                });
        }

        /// <summary>
        /// Helper to execute Sync Http tests
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param>   
        /// <param name="count">number to RDD calls to be made by the test application.  </param> 
        /// <param name="accessTimeMax">approximate maximum time taken by RDD Call.  </param> 
        private void ExecuteSyncHttpTests(TestWebApplication testWebApplication, bool success, int count, TimeSpan accessTimeMax)
        {
            testWebApplication.DoTest(
                application =>
                {
                    var queryString = success ? QueryStringOutboundHttp : QueryStringOutboundHttpFailed;
                    var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;
                    string commandNameExpected = string.Empty;
                    application.ExecuteAnonymousRequest(queryString + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();

                    // Validate the RDD Telemetry properties                    
                    Assert.AreEqual(
                        count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        if (DependencySourceType.Apmc == sourceExpected)
                        {
                            Assert.AreEqual("GET " + resourceNameExpected, httpItem.Data.BaseData.Name, "For StatusMonitor implementation we expect verb to be collected.");
                        }

                        this.ValidateRddTelemetryValues(httpItem, resourceNameExpected, commandNameExpected, accessTimeMax, success);
                    }
                });
        }

        private void ExecuteSyncHttpClientTests(TestWebApplication testWebApplication, TimeSpan accessTimeMax)
        {
            testWebApplication.DoTest(
                application =>
                {
                    var queryString = "?type=httpClient&count=1";
                    var resourceNameExpected = "http://www.google.com/404";
                    string commandNameExpected = string.Empty;
                    application.ExecuteAnonymousRequest(queryString);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();

                    // Validate the RDD Telemetry properties                    
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        if (DependencySourceType.Apmc == sourceExpected)
                        {
                            Assert.AreEqual("GET " + resourceNameExpected, httpItem.Data.BaseData.Name, "For StatusMonitor implementation we expect verb to be collected.");
                        }

                        this.ValidateRddTelemetryValues(httpItem, resourceNameExpected, commandNameExpected, accessTimeMax, false);
                    }
                });
        }

        /// <summary>
        /// Helper to execute Sync Http tests
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param>   
        /// <param name="count">number to RDD calls to be made by the test application.  </param> 
        /// <param name="accessTimeMax">approximate maximum time taken by RDD Call.  </param> 
        private void ExecuteSyncHttpPostTests(TestWebApplication testWebApplication, bool success, int count, TimeSpan accessTimeMax)
        {
            testWebApplication.DoTest(
                application =>
                {
                    var queryString = success ? QueryStringOutboundHttpPost : QueryStringOutboundHttpPostFailed;
                    var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;
                    string commandNameExpected = string.Empty;
                    application.ExecuteAnonymousRequest(queryString + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured
                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();
  
                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    foreach (var httpItem in httpItems)
                    {
                        this.ValidateRddTelemetryValues(httpItem, resourceNameExpected, commandNameExpected, accessTimeMax, success);

                        if (DependencySourceType.Apmc == sourceExpected)
                        {
                            Assert.AreEqual("POST " + resourceNameExpected, httpItem.Data.BaseData.Name, "For StatusMonitor implementation we expect verb to be collected.");
                        }
                    }
                });
        }        

        /// <summary>
        /// Helper to execute Async http test which uses Callbacks.
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param> 
        private void ExecuteAsyncWithCallbackTests(TestWebApplication testWebApplication, bool success)
        {
            var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;
            string commandNameExpected = string.Empty;

            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(success ? QueryStringOutboundHttpAsync4 : QueryStringOutboundHttpAsync4Failed);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();                    

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    this.ValidateRddTelemetryValues(httpItems[0], resourceNameExpected, commandNameExpected, AccessTimeMaxHttpInitial, success);
                });
        }

        /// <summary>
        /// Helper to execute Async http test which uses async,await pattern (.NET 4.5 or higher only)
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param> 
        private void ExecuteAsyncAwaitTests(TestWebApplication testWebApplication, bool success)
        {
            var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;
            string commandNameExpected = string.Empty;

            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(success ? QueryStringOutboundHttpAsyncAwait1 : QueryStringOutboundHttpAsyncAwait1Failed);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();                    

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    this.ValidateRddTelemetryValues(httpItems[0], resourceNameExpected, commandNameExpected, AccessTimeMaxHttpInitial, success); 
                });
        }

        /// <summary>
        /// Helper to execute Azure SDK tests.
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed.</param>
        /// <param name="count">number to RDD calls to be made by the test application.</param> 
        /// <param name="type"> type of azure call.</param> 
        /// <param name="expectedUrl">expected url for azure call.</param> 
        private void ExecuteAzureSDKTests(TestWebApplication testWebApplication, int count, string type, string expectedUrl)
        {
            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(string.Format(QueryStringOutboundAzureSdk, type, count));

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.Http).ToArray();                  
                    int countItem = 0;

                    foreach (var httpItem in httpItems)
                    {
                        var accessTime = httpItem.Data.BaseData.Value;
                        Assert.IsTrue(accessTime >= 0, "Access time should be above zero for azure calls");

                        var url = httpItem.Data.BaseData.Name;
                        if (url.Contains(expectedUrl))
                        {
                            countItem++;
                        }                        
                        else
                        {
                            Assert.Fail("ExecuteAzureSDKTests.url not matching for " + url);
                        }
                    }

                    Assert.IsTrue(countItem >= count, "Azure " + type + " access captured " + countItem + " is less than " + count);                    
                });
        }

        /// <summary>
        /// Validates Runtime Dependency Telemetry values.
        /// </summary>        
        /// <param name="itemToValidate">RDD Item to be validated.</param>
        /// <param name="remoteDependencyNameExpected">Expected name.</param>   
        /// <param name="accessTimeMax">Expected maximum limit for access time.</param>   
        /// <param name="successFlagExpected">Expected value for success flag.</param>   
        private void ValidateRddTelemetryValues(
            TelemetryItem<RemoteDependencyData> itemToValidate, 
            string remoteDependencyNameExpected, 
            string commandNameExpected, 
            TimeSpan accessTimeMax, 
            bool successFlagExpected)
        {
            if (itemToValidate.Data.BaseData.DependencyKind == DependencyKind.SQL)
            {
                // For http name is validated in test itself
                Assert.IsTrue(itemToValidate.Data.BaseData.Name.Contains(remoteDependencyNameExpected),
                    "The remote dependancy name is incorrect. Expected: " + remoteDependencyNameExpected +
                    ". Collected: " + itemToValidate.Data.BaseData.Name);
            }

            //If the command name is expected to be empty, the deserializer will make the CommandName null
            if (DependencySourceType.Apmc == sourceExpected)
            { 
                if (string.IsNullOrEmpty(commandNameExpected))
                    Assert.IsNull(itemToValidate.Data.BaseData.CommandName);
                else
                    Assert.IsTrue(itemToValidate.Data.BaseData.CommandName.Equals(commandNameExpected), "The command name is incorrect");
            }

            string actualSdkVersion = itemToValidate.InternalContext.SdkVersion;
            Assert.IsTrue(
                DependencySourceType.Apmc == sourceExpected
                    ? actualSdkVersion.Contains("rddp")
                    : actualSdkVersion.Contains("rddf"), "Actual version:" + actualSdkVersion);

            // Validate is within expected limits
            var ticks = (long)(itemToValidate.Data.BaseData.Value * 10000);

            var accessTime = TimeSpan.FromTicks(ticks);
            // DNS resolution may take up to 15 seconds https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.timeout(v=vs.110).aspx.
            // In future when tests will be refactored we should re-think failed http calls validation policy - need to validate resposnes that actually fails on GetResponse, 
            // not only those made to not-existing domain.
            var accessTimeMaxPlusDnsResolutionTime = accessTimeMax.Add(TimeSpan.FromSeconds(15));
            if (successFlagExpected)
            {
                Assert.IsTrue(accessTime.Ticks > 0, "Access time should be above zero");
            }
            else
            {
                Assert.IsTrue(accessTime.Ticks >= 0, "Access time should be zero or above for failed calls");
            }

            Assert.IsTrue(accessTime < accessTimeMaxPlusDnsResolutionTime, string.Format("Access time of {0} exceeds expected max of {1}", accessTime, accessTimeMaxPlusDnsResolutionTime));

            // Validate success and async flag values
            var successFlagActual = itemToValidate.Data.BaseData.Success;
            Assert.AreEqual(successFlagExpected, successFlagActual, "Success flag collected is wrong");
        }

        #endregion
    }
}
