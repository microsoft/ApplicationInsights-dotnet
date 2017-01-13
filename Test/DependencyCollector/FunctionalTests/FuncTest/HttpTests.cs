namespace FuncTest
{
    using System;
    using System.Linq;    
    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests RDD Functionality for a ASP.NET WebApplication in DOTNET 4.5.1 and DOTNET 4.6
    /// ASPX451 refers to the test application throughout the functional test context.
    /// The same app is used for testsing 4.5.1 and 4.6 scenarios.
    /// </summary>
    [TestClass]
    public class HttpTests
    {
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
        /// Resource Name for bing.
        /// </summary>
        private Uri ResourceNameHttpToBing = new Uri("http://www.bing.com");

        /// <summary>
        /// Resource Name for failed request.
        /// </summary>
        private Uri ResourceNameHttpToFailedRequest = new Uri("http://www.zzkaodkoakdahdjghejajdnad.com");

        /// <summary>
        /// Maximum access time for the initial call - This includes an additional 1-2 delay introduced before the very first call by Profiler V2.
        /// </summary>        
        private readonly TimeSpan AccessTimeMaxHttpInitial = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Maximum access time for calls after initial - This does not incur perf hit of the very first call.
        /// </summary>        
        private readonly TimeSpan AccessTimeMaxHttpNormal = TimeSpan.FromSeconds(3);
        
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DeploymentAndValidationTools.Initialize();

            AzureStorageHelper.Initialize();
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
            AzureStorageHelper.Cleanup();

            DeploymentAndValidationTools.CleanUp();
        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            DeploymentAndValidationTools.SdkEventListener.Start();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            Assert.IsFalse(DeploymentAndValidationTools.SdkEventListener.FailureDetected, "Failure is detected. Please read test output logs.");
            DeploymentAndValidationTools.SdkEventListener.Stop();
        }

        #region 451

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForSyncHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            // Execute and verify calls which succeeds            
            this.ExecuteSyncHttpTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal);            
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForSyncHttpPostCallAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            // Execute and verify calls which succeeds            
            this.ExecuteSyncHttpPostTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForSyncHttpFailedAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            // Execute and verify calls which fails.            
            this.ExecuteSyncHttpTests(DeploymentAndValidationTools.Aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial);            
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAsync1HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }            
            this.ExecuteAsyncTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal, QueryStringOutboundHttpAsync1);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForHttpAspx451WithHttpClient()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteSyncHttpClientTests(DeploymentAndValidationTools.Aspx451TestWebApplication, AccessTimeMaxHttpNormal);
        }

        [TestMethod]
        [Description("Verify RDD is collected for failed Async Http Calls in ASPX 4.5.1 application")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForFailedAsync1HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncTests(DeploymentAndValidationTools.Aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial, QueryStringOutboundHttpAsync1Failed);            
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAsync2HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
            this.ExecuteAsyncTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal, QueryStringOutboundHttpAsync2);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForFailedAsync2HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncTests(DeploymentAndValidationTools.Aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial, QueryStringOutboundHttpAsync2Failed);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAsync3HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
            this.ExecuteAsyncTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true, 1, AccessTimeMaxHttpNormal, QueryStringOutboundHttpAsync3);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForFailedAsync3HttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncTests(DeploymentAndValidationTools.Aspx451TestWebApplication, false, 1, AccessTimeMaxHttpInitial, QueryStringOutboundHttpAsync3Failed);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAsyncWithCallBackHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncWithCallbackTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAsyncAwaitHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncAwaitTests(DeploymentAndValidationTools.Aspx451TestWebApplication, true);
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForFailedAsyncAwaitHttpAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAsyncAwaitTests(DeploymentAndValidationTools.Aspx451TestWebApplication, false);
        }        

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAzureSdkBlobAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAzureSDKTests(DeploymentAndValidationTools.Aspx451TestWebApplication, 1, "blob", "http://127.0.0.1:11000");           
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAzureSdkQueueAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAzureSDKTests(DeploymentAndValidationTools.Aspx451TestWebApplication, 1, "queue", "http://127.0.0.1:11001");           
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForAzureSdkTableAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteAzureSDKTests(DeploymentAndValidationTools.Aspx451TestWebApplication, 1, "table", "http://127.0.0.1:11002");           
        }

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolderWin32)]
        public void TestRddForWin32ApplicationPool()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }
            this.ExecuteSyncHttpTests(DeploymentAndValidationTools.Aspx451TestWebApplicationWin32, true, 1, AccessTimeMaxHttpInitial);
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
                        DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(
                            DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);

                    var httpItems =
                        allItems.Where(i => i.data.baseData.type == "Http").ToArray();

                    Assert.AreEqual(
                        3*count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        this.Validate(httpItem, resourceNameExpected, accessTimeMax, success, verb: "GET");
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
                    application.ExecuteAnonymousRequest(queryString + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured
                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();

                    Assert.AreEqual(
                        count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        this.Validate(httpItem, resourceNameExpected, accessTimeMax, success, verb: "GET");
                    }
                });
        }

        private void ExecuteSyncHttpClientTests(TestWebApplication testWebApplication, TimeSpan accessTimeMax)
        {
            testWebApplication.DoTest(
                application =>
                {
                    var queryString = "?type=httpClient&count=1";
                    var resourceNameExpected = new Uri("http://www.google.com/404");
                    application.ExecuteAnonymousRequest(queryString);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured
                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();

                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        this.Validate(httpItem, resourceNameExpected, accessTimeMax, successFlagExpected: false, verb: "GET");
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
                    application.ExecuteAnonymousRequest(queryString + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured
                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();
  
                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        this.Validate(httpItem, resourceNameExpected, accessTimeMax, success, verb: "POST");
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
            
            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(success ? QueryStringOutboundHttpAsync4 : QueryStringOutboundHttpAsync4Failed);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();                    

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    this.Validate(httpItems[0], resourceNameExpected, AccessTimeMaxHttpInitial, success, "GET");
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
            
            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(success ? QueryStringOutboundHttpAsyncAwait1 : QueryStringOutboundHttpAsyncAwait1Failed);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();                    

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    this.Validate(httpItems[0], resourceNameExpected, AccessTimeMaxHttpInitial, success, "GET"); 
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
                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();
                    int countItem = 0;

                    foreach (var httpItem in httpItems)
                    {
                        TimeSpan accessTime = TimeSpan.Parse(httpItem.data.baseData.duration);
                        Assert.IsTrue(accessTime.TotalMilliseconds >= 0, "Access time should be above zero for azure calls");

                        string actualSdkVersion = httpItem.tags[new ContextTagKeys().InternalSdkVersion];
                        Assert.IsTrue(actualSdkVersion.Contains(DeploymentAndValidationTools.ExpectedSDKPrefix), "Actual version:" + actualSdkVersion);

                        var url = httpItem.data.baseData.data;
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

        #endregion

        private void Validate(TelemetryItem<RemoteDependencyData> itemToValidate,
            Uri expectedUrl,
            TimeSpan accessTimeMax,
            bool successFlagExpected,
            string verb)
        {
            if ("rddp" == DeploymentAndValidationTools.ExpectedSDKPrefix)
            {
                Assert.AreEqual(verb + " " + expectedUrl.AbsolutePath, itemToValidate.data.baseData.name, "For StatusMonitor implementation we expect verb to be collected.");
                Assert.AreEqual(expectedUrl.Host, itemToValidate.data.baseData.target);
                Assert.AreEqual(expectedUrl.OriginalString, itemToValidate.data.baseData.data);
            }

            DeploymentAndValidationTools.Validate(itemToValidate, accessTimeMax, successFlagExpected);
        }
    }
}
