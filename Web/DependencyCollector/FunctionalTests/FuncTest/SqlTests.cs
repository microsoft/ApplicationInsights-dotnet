namespace FuncTest
{
    using System.Linq;
    using System.Threading;

    using Microsoft.Developer.Analytics.DataCollection.Model.v1;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using RemoteDependencyKind = Microsoft.Developer.Analytics.DataCollection.Model.v2.DependencyKind;
    using System;
    public partial class RddTests
    {
        private const string RddItemNameValue = "data.item.value";

        /// <summary>
        /// Label used by test app to identify the query being executed.
        /// </summary> 
        private const string QueryToExecuteLabel = "Query Executed:";

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
                     (application) =>
                     {
                         application.ExecuteAnonymousRequest(queryString);

                         //// The above request would have trigged RDD module to monitor and create RDD telemetry
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                         var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");
                         this.ValidateRddTelemetryValues(sqlItems[0], ResourceNameSQLToDevApm + " | " + StoredProcedureName, StoredProcedureName, 1, TimeSpan.FromSeconds(10), true, true);
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
                     (application) =>
                     {
                         application.ExecuteAnonymousRequest("?type=TestExecuteReaderTwice&count=1");

                         //// The above request would have trigged APMC into action and APMC should have collected RDD telemtry and 
                         //// sent to EventSource, from where AIC/SDK picks it up and sends to the fake end point.
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured
                         //// Sleep for some secs to give SDK the time to sent the events to Fake DataPlatform.
                         //Thread.Sleep(SleepTimeForSdkToSendEvents);

                         //var allItems = platform.GetAllReceivedDataItems();
                         //var sqlItems = ExtractRddItems(allItems, RemoteDependencyKind.SQL, 0);
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
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", true, true);
        }

        [Description("Verifying SqlCommand.BeginExecuteReader monitoring failed call.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequenceFailed()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", false, true);
        }

        [Description("Verifying when two simulatinious asyncronous operations are executed we are not reporting any data as it could be wrong.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceWithTasks()
        {
            aspx451TestWebApplication.DoTest(
                     (application) =>
                     {
                         application.ExecuteAnonymousRequest("?type=TestExecuteReaderTwiceWithTasks&count=1");

                         //// The above request would have trigged RDD module to monitor and create RDD telemetry
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                         var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();
                         Assert.AreEqual(0, sqlItems.Length, "We must not report any rdd data as it could be wrong");
                     });
        }

        [Description("Verifying async SqlCommand.ExecuteReader monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", true, true);
        }

        [Description("Verifying async SqlCommand.ExecuteReader monitoring in failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", false, true);
        }

        [Description("Verifying SqlCommand.BeginExecuteReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReader()
        {
            this.TestSqlCommandExecute("BeginExecuteReader", true, true);
        }

        [Description("Verifying SqlCommand.BeginExecuteReader monitoring in failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteReader", false, true);
        }

        [Description("Verifying async SqlCommand.ExecuteScalar monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsync()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", true, true);
        }

        [Description("Verifying async SqlCommand.ExecuteScalar monitoring in failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", false, true);
        }

        [Description("Verifying async SqlCommand.ExecuteNonQuery monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsync()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", true, true);
        }

        [Description("Verifying async SqlCommand.ExecuteNonQuery monitoring for failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", false, true);
        }

        [Description("Verifying SqlCommand.BeginExecuteNonQuery monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQuery()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery", true, true);
        }

        [Description("Verifying SqlCommand.BeginExecuteNonQuery monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery", false, true);
        }

        [Description("Verifying async SqlCommand.ExecuteXmlReader monitoring")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", true, true);
        }

        [Description("Verifying async SqlCommand.ExecuteXmlReader monitoring for failed calls")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", false, true, ForXMLClauseInFailureCase);
        }

        [Description("Verifying SqlCommand.BeginExecuteXmlReader monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteXmlReader", false, true);
        }

        [Description("Verifying SqlCommand.ExecuteScalar monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalar()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", true, false);
        }

        [Description("Verifying SqlCommand.ExecuteScalar monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalarFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", false, false);
        }

        [Description("Verifying SqlCommand.ExecuteNonQuery monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQuery()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", true, false);
        }

        [Description("Verifying SqlCommand.ExecuteNonQuery monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", false, false);
        }

        [Description("Verifying SqlCommand.ExecuteReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader", true, false);
        }

        [Description("Verifying SqlCommand.ExecuteReader monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader", false, false);
        }

        [Description("Verifying SqlCommand.ExecuteXmlReader monitoring.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", true, false);
        }

        [Description("Verifying SqlCommand.ExecuteXmlReader monitoring for failed calls.")]
        [Owner("mihailsm")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", false, false);
        }

        private void TestSqlCommandExecute(string type, bool success, bool async, string extraClauseForFailureCase = null)
        {
            aspx451TestWebApplication.DoTest(
                 (application) =>
                 {
                     string responseForQueryValidation = application.ExecuteAnonymousRequest("?type=" + type + "&count=1" + "&success=" + success);

                     //// The above request would have trigged RDD module to monitor and create RDD telemetry
                     //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      

                     var allItems = sdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(SleepTimeForSdkToSendEvents);
                     var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();                     
                     Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");

                     string queryToValidate = (true == success) ? string.Empty : InvalidSqlQueryToApmDatabase + extraClauseForFailureCase;
                     if (!string.IsNullOrEmpty(responseForQueryValidation))
                     {
                         int placeToStart = responseForQueryValidation.IndexOf(QueryToExecuteLabel, StringComparison.OrdinalIgnoreCase) + QueryToExecuteLabel.Length;
                         int restOfLine = responseForQueryValidation.IndexOf(System.Environment.NewLine, StringComparison.OrdinalIgnoreCase) - placeToStart;
                         queryToValidate = responseForQueryValidation.Substring(placeToStart, restOfLine);
                     }

                     this.ValidateRddTelemetryValues(sqlItems[0], ResourceNameSQLToDevApm, queryToValidate, 1, TimeSpan.FromSeconds(20), success, async);
                 });
        }         
    } 
}
