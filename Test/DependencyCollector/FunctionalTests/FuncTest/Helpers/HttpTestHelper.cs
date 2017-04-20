using System;
using System.Linq;
using AI;
using FuncTest.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace FuncTest.Helpers
{
    internal class HttpTestHelper
    {
        /// <summary>
        /// Resource Name for bing.
        /// </summary>
        private static Uri ResourceNameHttpToBing = new Uri("http://www.bing.com");

        /// <summary>
        /// Resource Name for failed request.
        /// </summary>
        private static Uri ResourceNameHttpToFailedRequest = new Uri("http://google.com/404");

        /// <summary>
        /// Resource Name for failed at DNS request.
        /// </summary>
        internal static Uri ResourceNameHttpToFailedAtDnsRequest = new Uri("http://abcdefzzzzeeeeadadad.com");

        /// <summary>
        /// Helper to execute Async Http tests.
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed.</param>
        /// <param name="success">Indicates if the tests should test success or failure case.</param> 
        /// <param name="count">Number to RDD calls to be made by the test application. </param> 
        /// <param name="accessTimeMax">Approximate maximum time taken by RDD Call.  </param>
        /// <param name="url">url</param> 
        internal static void ExecuteAsyncTests(TestWebApplication testWebApplication, bool success, int count,
            TimeSpan accessTimeMax, string url, string resultCodeExpected)
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
                        3 * count,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    foreach (var httpItem in httpItems)
                    {
                        Validate(httpItem, resourceNameExpected, accessTimeMax, success, "GET", resultCodeExpected);
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
        public static void ExecuteSyncHttpTests(TestWebApplication testWebApplication, bool success, int count, TimeSpan accessTimeMax,
            string resultCodeExpected, string queryString)
        {
            testWebApplication.DoTest(
                application =>
                {                    
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
                        Validate(httpItem, resourceNameExpected, accessTimeMax, success, "GET", resultCodeExpected);
                    }
                });
        }

        public static void ExecuteSyncHttpClientTests(TestWebApplication testWebApplication, TimeSpan accessTimeMax, string resultCodeExpected)
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
                        // This is a call to google.com/404 which will fail but typically takes longer time. So accesstime can more than normal.
                        Validate(httpItem, resourceNameExpected, accessTimeMax.Add(TimeSpan.FromSeconds(15)), false, "GET", resultCodeExpected);
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
        public static void ExecuteSyncHttpPostTests(TestWebApplication testWebApplication, bool success, int count, TimeSpan accessTimeMax, string resultCodeExpected, string queryString)
        {
            testWebApplication.DoTest(
                application =>
                {                    
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
                        Validate(httpItem, resourceNameExpected, accessTimeMax, success, "POST", resultCodeExpected);
                    }
                });
        }

        /// <summary>
        /// Helper to execute Async http test which uses Callbacks.
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param> 
        public static void ExecuteAsyncWithCallbackTests(TestWebApplication testWebApplication, bool success, TimeSpan accessTimeMax, string resultCodeExpected, string queryString)
        {
            var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;

            testWebApplication.DoTest(
                application =>
                {
                application.ExecuteAnonymousRequest(queryString);                        

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    Validate(httpItems[0], resourceNameExpected, accessTimeMax, success, "GET", resultCodeExpected);
                });
        }

        /// <summary>
        /// Helper to execute Async http test which uses async,await pattern (.NET 4.5 or higher only)
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param> 
        public static void ExecuteAsyncAwaitTests(TestWebApplication testWebApplication, bool success, TimeSpan accessTimeMax, string resultCodeExpected, string queryString)
        {
            var resourceNameExpected = success ? ResourceNameHttpToBing : ResourceNameHttpToFailedRequest;

            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(queryString);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();

                    // Validate the RDD Telemetry properties
                    Assert.AreEqual(
                        1,
                        httpItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");
                    Validate(httpItems[0], resourceNameExpected, accessTimeMax, success, "GET", resultCodeExpected);
                });
        }

        /// <summary>
        /// Helper to execute Azure SDK tests.
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed.</param>
        /// <param name="count">number to RDD calls to be made by the test application.</param> 
        /// <param name="type"> type of azure call.</param> 
        /// <param name="expectedUrl">expected url for azure call.</param> 
        public static void ExecuteAzureSDKTests(TestWebApplication testWebApplication, int count, string type, string expectedUrl, string queryString)
        {
            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(string.Format(CultureInfo.InvariantCulture, queryString, type, count));

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var httpItems = allItems.Where(i => i.data.baseData.type == "Http").ToArray();
                    int countItem = 0;

                    foreach (var httpItem in httpItems)
                    {
                        TimeSpan accessTime = TimeSpan.Parse(httpItem.data.baseData.duration, CultureInfo.InvariantCulture);
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

        public static void Validate(TelemetryItem<RemoteDependencyData> itemToValidate,
            Uri expectedUrl,
            TimeSpan accessTimeMax,
            bool successFlagExpected,
            string verb,
            string resultCodeExpected)
        {
            if ("rddp" == DeploymentAndValidationTools.ExpectedSDKPrefix)
            {
                Assert.AreEqual(verb + " " + expectedUrl.AbsolutePath, itemToValidate.data.baseData.name, "For StatusMonitor implementation we expect verb to be collected.");
                Assert.AreEqual(expectedUrl.Host, itemToValidate.data.baseData.target);
                Assert.AreEqual(expectedUrl.OriginalString, itemToValidate.data.baseData.data);
            }

            DeploymentAndValidationTools.Validate(itemToValidate, accessTimeMax, successFlagExpected, resultCodeExpected);
        }        
    }
}
