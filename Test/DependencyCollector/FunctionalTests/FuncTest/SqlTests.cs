namespace FuncTest
{
    using System;
    using System.Linq;

    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using RemoteDependencyKind = Microsoft.Developer.Analytics.DataCollection.Model.v2.DependencyKind;
    

    public partial class RddTests
    {
        /// <summary>
        /// Label used by test app to identify the query being executed.
        /// </summary> 
        private const string QueryToExecuteLabel = "Query Executed:";

        /// <summary>
        /// Tests RDD events are generated for external dependency call - Sync SQL calls, made in a ASP.NET 4.5.1 Application
        /// </summary>
        [TestMethod]
        [Description("Verify RDD is collected for Sync Sql Calls in ASPX 4.5.1 application")]
        [Owner("cithomas")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        public void TestRddForSyncSqlAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteSyncSqlTests(aspx451TestWebApplication, 1, AccessTimeMaxSqlCallToApmdbNormal);
        }

        /// <summary>
        /// Verifying colecting stored procedure name in async calls
        /// </summary>
        [Description("Verifying colecting stored procedure name in async calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestStoredProcedureNameIsCollected()
        {
            const string StoredProcedureName = "GetTopTenMessages";
            string queryString = "?type=ExecuteReaderStoredProcedureAsync&count=1&storedProcedureName=" + StoredProcedureName;

            aspx451TestWebApplication.DoTest(
                     application =>
                     {
                         application.ExecuteAnonymousRequest(queryString);

                         //// The above request would have trigged RDD module to monitor and create RDD telemetry
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                         var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");
                         this.ValidateRddTelemetryValues(sqlItems[0], ResourceNameSQLToDevApm + " | " + StoredProcedureName, StoredProcedureName, TimeSpan.FromSeconds(10), true);
                     });           
        }

        [Description("Verifying that executing two commands simultaniously is not unhandled exceptions.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwice()
        {
            aspx451TestWebApplication.DoTest(
                     application =>
                     {
                         application.ExecuteAnonymousRequest("?type=TestExecuteReaderTwice&count=1");

                         var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();

                         Assert.AreEqual(0, sqlItems.Length, "We don't have to collect any rdd as it is impossible to execute on the same command two async methods at the same time");
                     });
        }


        [Description("Verifying SqlCommand.BeginExecuteReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequence()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", true);
        }

        [Description("Verifying SqlCommand.BeginExecuteReader monitoring failed call.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequenceFailed()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", false);
        }

        [Description("Verifying when two simulatinious asyncronous operations are executed we are not reporting any data as it could be wrong.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceWithTasks()
        {
            aspx451TestWebApplication.DoTest(
                     application =>
                     {
                         application.ExecuteAnonymousRequest("?type=TestExecuteReaderTwiceWithTasks&count=1");

                         //// The above request would have trigged RDD module to monitor and create RDD telemetry
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                         var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "We should only report 1 dependency call");
                     });
        }

        [Description("Verifying async SqlCommand.ExecuteReader monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", true);
        }

        [Description("Verifying async SqlCommand.ExecuteReader monitoring in failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", false);
        }

        [Description("Verifying SqlCommand.BeginExecuteReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReader()
        {
            this.TestSqlCommandExecute("BeginExecuteReader", true);
        }

        [Description("Verifying SqlCommand.BeginExecuteReader monitoring in failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteReader", false);
        }

        [Description("Verifying async SqlCommand.ExecuteScalar monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsync()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", true);
        }

        [Description("Verifying async SqlCommand.ExecuteScalar monitoring in failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", false);
        }

        [Description("Verifying async SqlCommand.ExecuteNonQuery monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsync()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", true);
        }

        [Description("Verifying async SqlCommand.ExecuteNonQuery monitoring for failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", false);
        }

        [Description("Verifying SqlCommand.BeginExecuteNonQuery monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQuery()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery", true);
        }

        [Description("Verifying SqlCommand.BeginExecuteNonQuery monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery", false);
        }

        [Description("Verifying async SqlCommand.ExecuteXmlReader monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", true);
        }

        [Description("Verifying async SqlCommand.ExecuteXmlReader monitoring for failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", false, ForXMLClauseInFailureCase);
        }

        [Description("Verifying SqlCommand.BeginExecuteXmlReader monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteXmlReader", false);
        }

        [Description("Verifying SqlCommand.ExecuteScalar monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalar()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", true);
        }

        [Description("Verifying SqlCommand.ExecuteScalar monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalarFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", false);
        }

        [Description("Verifying SqlCommand.ExecuteNonQuery monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQuery()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", true);
        }

        [Description("Verifying SqlCommand.ExecuteNonQuery monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", false);
        }

        [Description("Verifying SqlCommand.ExecuteReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader", true);
        }

        [Description("Verifying SqlCommand.ExecuteReader monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader", false);
        }

        [Description("Verifying SqlCommand.ExecuteXmlReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", true);
        }

        [Description("Verifying SqlCommand.ExecuteXmlReader monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", false);
        }

        private void TestSqlCommandExecute(string type, bool success, string extraClauseForFailureCase = null)
        {
            aspx451TestWebApplication.DoTest(
                 application =>
                 {
                     string responseForQueryValidation = application.ExecuteAnonymousRequest("?type=" + type + "&count=1" + "&success=" + success);

                     //// The above request would have trigged RDD module to monitor and create RDD telemetry
                     //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      

                     var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                     var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();                     
                     Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");

                     string queryToValidate = success ? string.Empty : InvalidSqlQueryToApmDatabase + extraClauseForFailureCase;
                     if (!string.IsNullOrEmpty(responseForQueryValidation))
                     {
                         int placeToStart = responseForQueryValidation.IndexOf(QueryToExecuteLabel, StringComparison.OrdinalIgnoreCase) + QueryToExecuteLabel.Length;
                         int restOfLine = responseForQueryValidation.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase) - placeToStart;
                         queryToValidate = responseForQueryValidation.Substring(placeToStart, restOfLine);
                     }

                     this.ValidateRddTelemetryValues(sqlItems[0], ResourceNameSQLToDevApm, queryToValidate, TimeSpan.FromSeconds(20), success);
                 });
        }

        /// <summary>
        /// Helper to execute Sync Http tests
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="count">number to RDD calls to be made by the test application </param> 
        /// <param name="accessTimeMax">approximate maximum time taken by RDD Call.  </param> 
        private void ExecuteSyncSqlTests(TestWebApplication testWebApplication, int count, TimeSpan accessTimeMax)
        {
            this.ExecuteSyncSqlTests(testWebApplication, count, count, accessTimeMax);
        }

        /// <summary>
        /// Helper to execute Sync Http tests
        /// </summary>
        /// <param name="testWebApplication">The test application for which tests are to be executed</param>
        /// <param name="expectedCount">number of expected RDD calls to be made by the test application </param> 
        /// <param name="count">number to RDD calls to be made by the test application </param> 
        /// <param name="accessTimeMax">approximate maximum time taken by RDD Call.  </param> 
        private void ExecuteSyncSqlTests(TestWebApplication testWebApplication, int expectedCount, int count, TimeSpan accessTimeMax)
        {
            testWebApplication.DoTest(
                application =>
                {
                    application.ExecuteAnonymousRequest(QueryStringOutboundSql + count);

                    //// The above request would have trigged RDD module to monitor and create RDD telemetry
                    //// Listen in the fake endpoint and see if the RDDTelemtry is captured

                    var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                    var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();


                    Assert.AreEqual(
                        expectedCount,
                        sqlItems.Length,
                        "Total Count of Remote Dependency items for SQL collected is wrong.");

                    foreach (var sqlItem in sqlItems)
                    {
                        string spName = "GetTopTenMessages";
                        this.ValidateRddTelemetryValues(sqlItem, ResourceNameSQLToDevApm + " | " + spName, spName, accessTimeMax, true);
                    }
                });
        }
    } 
}
