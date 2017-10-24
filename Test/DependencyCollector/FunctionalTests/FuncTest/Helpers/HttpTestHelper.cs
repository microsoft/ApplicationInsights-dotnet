using System;
using System.Diagnostics.CodeAnalysis;
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
        private static Uri ResourceNameHttpToBing = new Uri("https://www.bing.com");

        /// <summary>
        /// Resource Name for failed request.
        /// </summary>
        private static Uri ResourceNameHttpToFailedRequest = new Uri("https://google.com/404");

        /// <summary>
        /// Resource Name for failed at DNS request.
        /// </summary>
        internal static Uri ResourceNameHttpToFailedAtDnsRequest = new Uri("https://abcdefzzzzeeeeadadad.com");

        /// <summary>
        /// Resource Name for dev database.
        /// </summary>
        private const string ResourceNameSQLToDevApm = @".\SQLEXPRESS | RDDTestDatabase";                
        
        /// <summary>
        /// Valid SQL Query. The wait for delay of 6 ms is used to prevent access time of less than 1 ms. SQL is not accurate below 3, so used 6 ms delay.
        /// </summary> 
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        private const string ValidSqlQueryToApmDatabase = "WAITFOR DELAY '00:00:00:006'; select * from dbo.Messages";

        /// <summary>
        /// Valid SQL Query to get count.
        /// </summary> 
        private const string ValidSqlQueryCountToApmDatabase = "WAITFOR DELAY '00:00:00:006'; SELECT count(*) FROM dbo.Messages";

        /// <summary>
        /// Invalid SQL query only needed here because the test web app we use to run queries will throw a 500 and we can't get back the invalid query from it.
        /// </summary>        
        private const string InvalidSqlQueryToApmDatabase = "SELECT TOP 2 * FROM apm.[Database1212121]";

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
                    var resourceNameExpected = new Uri("https://www.google.com/404");
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
        public static void ExecuteAzureSDKTests(TestWebApplication testWebApplication, int count, string type, string expectedUrl, string queryString, bool checkStatus)
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
                        Assert.IsTrue(actualSdkVersion.Contains(DeploymentAndValidationTools.ExpectedHttpSDKPrefix), "Actual version:" + actualSdkVersion);

                        var url = httpItem.data.baseData.data;
                        if (url.Contains(expectedUrl))
                        {
                            countItem++;
                        }
                        else
                        {
                            Assert.Fail("ExecuteAzureSDKTests.url not matching for " + url);
                        }

                        var successFlagActual = httpItem.data.baseData.success;
                        if (checkStatus)
                        {
                            Assert.AreEqual(true, successFlagActual, "Success flag collected is wrong.It is expected to be true for all azure sdk calls.");
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
            if ("rddf" != DeploymentAndValidationTools.ExpectedHttpSDKPrefix)
            {
                Assert.AreEqual(verb + " " + expectedUrl.AbsolutePath, itemToValidate.data.baseData.name, "For StatusMonitor implementation we expect verb to be collected.");
                Assert.AreEqual(expectedUrl.Host, itemToValidate.data.baseData.target);
                Assert.AreEqual(expectedUrl.OriginalString, itemToValidate.data.baseData.data);
            }

            DeploymentAndValidationTools.Validate(itemToValidate, accessTimeMax, successFlagExpected, resultCodeExpected);
        }

        /// <summary>
        /// Helper to execute Sync SQL tests
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="success">indicates if the tests should test success or failure case</param>   
        /// <param name="count">number to RDD calls to be made by the test application.  </param> 
        /// <param name="accessTimeMax">approximate maximum time taken by RDD Call.  </param>
        /// <param name="queryString">The query string. </param> 
        public static void ExecuteSqlTest(
            TestWebApplication testWebApplication, bool success, int count, TimeSpan accessTimeMax, string queryString)
        {
            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(queryString + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured
                    var allItems = DeploymentAndValidationTools.SdkEventListener
                        .ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);

                    var sqlItems = allItems.Where(i => i.data.baseData.type == "SQL").ToArray();

                    Assert.AreEqual(
                        count,
                        sqlItems.Length,
                        "Total Count of Remote Dependency items for HTTP collected is wrong.");

                    var validQuery 
                        = queryString.Contains("CommandExecuteScalar")
                            ? ValidSqlQueryCountToApmDatabase
                            : ValidSqlQueryToApmDatabase;

                    var queryToValidate 
                        = success 
                            ? queryString.Contains("StoredProcedure")
                                ? "GetTopTenMessages"
                                : queryString.Contains("Xml")
                                    ? validQuery + " FOR XML AUTO"   
                                    : validQuery
                            : InvalidSqlQueryToApmDatabase;

                    foreach (var sqlItem in sqlItems)
                    {
                        Validate(
                            sqlItem,
                            ResourceNameSQLToDevApm,
                            queryToValidate,
                            TimeSpan.FromSeconds(20),
                            successFlagExpected: success,
                            sqlErrorCodeExpected: success ? string.Empty : "208",
                            sqlErrorMessageExpected: null);
                    }
                });
        }

        private static void Validate(TelemetryItem<RemoteDependencyData> itemToValidate,
            string targetExpected,
            string commandNameExpected,
            TimeSpan accessTimeMax,
            bool successFlagExpected,
            string sqlErrorCodeExpected,
            string sqlErrorMessageExpected)
        {
            Assert.IsTrue(itemToValidate.data.baseData.target.Contains(targetExpected),
                "The remote dependancy target is incorrect. Expected: " + targetExpected +
                ". Collected: " + itemToValidate.data.baseData.target);

            Assert.AreEqual(sqlErrorCodeExpected, itemToValidate.data.baseData.resultCode);

            //If the command name is expected to be empty, the deserializer will make the CommandName null
            if ("rdddsc" == DeploymentAndValidationTools.ExpectedSqlSDKPrefix)
            {
                // Additional checks for profiler collection
                if (!string.IsNullOrEmpty(sqlErrorMessageExpected))
                {
                    Assert.AreEqual(sqlErrorMessageExpected, itemToValidate.data.baseData.properties["ErrorMessage"]);
                }

                if (string.IsNullOrEmpty(commandNameExpected))
                {
                    Assert.IsNull(itemToValidate.data.baseData.data);
                }
                else
                {
                    Assert.AreEqual(commandNameExpected, itemToValidate.data.baseData.data, "The command name is incorrect");
                }
            }

            DeploymentAndValidationTools.Validate(itemToValidate, accessTimeMax, successFlagExpected, sqlErrorCodeExpected);
        }
    }
}
