namespace FuncTest
{
    using System;
    using System.Linq;

    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using RemoteDependencyKind = Microsoft.Developer.Analytics.DataCollection.Model.v2.DependencyKind;
    
    [TestClass]
    public class SqlTests
    {
        /// <summary>
        /// Label used by test app to identify the query being executed.
        /// </summary> 
        private const string QueryToExecuteLabel = "Query Executed:";

        /// <summary>
        /// Resource Name for dev database.
        /// </summary>
        private const string ResourceNameSQLToDevApm = @".\SQLEXPRESS | RDDTestDatabase";

        /// <summary>
        /// Invalid SQL query only needed here because the test web app we use to run queries will throw a 500 and we can't get back the invalid query from it.
        /// </summary>        
        private const string InvalidSqlQueryToApmDatabase = "SELECT TOP 2 * FROM apm.[Database1212121]";

        /// <summary>
        /// Clause to go on end of SQL query when running XML query - only used in the failure case.
        /// </summary>        
        private const string ForXMLClauseInFailureCase = " FOR XML AUTO";

        /// <summary>
        /// Query string to specify Outbound SQL Call. 
        /// </summary>
        private const string QueryStringOutboundSql = "?type=sql&count=";

        /// <summary>
        /// Maximum access time for calls after initial - This does not incur perf hit of the very first call.
        /// </summary>        
        private readonly TimeSpan AccessTimeMaxSqlCallToApmdbNormal = TimeSpan.FromSeconds(5);

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            DeploymentAndValidationTools.Initialize();

            LocalDb.CreateLocalDb("RDDTestDatabase", DeploymentAndValidationTools.Aspx451TestWebApplication.AppFolder + "\\TestDatabase.sql");
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {
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

        [TestMethod]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForSyncSqlAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteSyncSqlTests(DeploymentAndValidationTools.Aspx451TestWebApplication, 1, AccessTimeMaxSqlCallToApmdbNormal);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestStoredProcedureNameIsCollected()
        {
            const string StoredProcedureName = "GetTopTenMessages";
            string queryString = "?type=ExecuteReaderStoredProcedureAsync&count=1&storedProcedureName=" + StoredProcedureName;

            DeploymentAndValidationTools.Aspx451TestWebApplication.DoTest(
                     application =>
                     {
                         application.ExecuteAnonymousRequest(queryString);

                         //// The above request would have trigged RDD module to monitor and create RDD telemetry
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                         var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");
                         this.Validate(
                             sqlItems[0], 
                             ResourceNameSQLToDevApm + " | " + StoredProcedureName, 
                             StoredProcedureName, 
                             TimeSpan.FromSeconds(10), 
                             successFlagExpected: true,
                             sqlErrorCodeExpected: "0",
                             sqlErrorMessageExpected: null);
                     });           
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwice()
        {
            DeploymentAndValidationTools.Aspx451TestWebApplication.DoTest(
                     application =>
                     {
                         application.ExecuteAnonymousRequest("?type=TestExecuteReaderTwice&count=1");

                         var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();

                         Assert.AreEqual(0, sqlItems.Length, "We don't have to collect any rdd as it is impossible to execute on the same command two async methods at the same time");
                     });
        }


        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequence()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequenceFailed()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceWithTasks()
        {
            DeploymentAndValidationTools.Aspx451TestWebApplication.DoTest(
                     application =>
                     {
                         application.ExecuteAnonymousRequest("?type=TestExecuteReaderTwiceWithTasks&count=1");

                         //// The above request would have trigged RDD module to monitor and create RDD telemetry
                         //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      
                         var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                         var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "We should only report 1 dependency call");
                     });
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReader()
        {
            this.TestSqlCommandExecute("BeginExecuteReader", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteReader", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsync()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsync()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQuery()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.", extraClauseForFailureCase: ForXMLClauseInFailureCase);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteXmlReader", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalar()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalarFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQuery()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        private void TestSqlCommandExecute(string type, string errorNumber, string errorMessage, string extraClauseForFailureCase = null)
        {
            DeploymentAndValidationTools.Aspx451TestWebApplication.DoTest(
                 application =>
                 {
                     bool success = errorNumber == "0";
                     string responseForQueryValidation = application.ExecuteAnonymousRequest("?type=" + type + "&count=1" + "&success=" + success);

                     //// The above request would have trigged RDD module to monitor and create RDD telemetry
                     //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      

                     var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                     var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();                     
                     Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");
                     
                     string queryToValidate = success ? string.Empty : InvalidSqlQueryToApmDatabase + extraClauseForFailureCase;
                     if (!string.IsNullOrEmpty(responseForQueryValidation))
                     {
                         int placeToStart = responseForQueryValidation.IndexOf(QueryToExecuteLabel, StringComparison.OrdinalIgnoreCase) + QueryToExecuteLabel.Length;
                         int restOfLine = responseForQueryValidation.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase) - placeToStart;
                         queryToValidate = responseForQueryValidation.Substring(placeToStart, restOfLine);
                     }

                     this.Validate(
                         sqlItems[0], 
                         ResourceNameSQLToDevApm, 
                         queryToValidate, 
                         TimeSpan.FromSeconds(20), 
                         successFlagExpected: success,
                         sqlErrorCodeExpected: errorNumber,
                         sqlErrorMessageExpected: errorMessage);
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

                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                    var sqlItems = allItems.Where(i => i.Data.BaseData.DependencyKind == RemoteDependencyKind.SQL).ToArray();


                    Assert.AreEqual(
                        expectedCount,
                        sqlItems.Length,
                        "Total Count of Remote Dependency items for SQL collected is wrong.");

                    foreach (var sqlItem in sqlItems)
                    {
                        string spName = "GetTopTenMessages";
                        this.Validate(
                            sqlItem, 
                            ResourceNameSQLToDevApm + " | " + spName, 
                            spName, 
                            accessTimeMax, successFlagExpected: true,
                            sqlErrorCodeExpected: "0",
                            sqlErrorMessageExpected: null);
                    }
                });
        }

        private void Validate(TelemetryItem<RemoteDependencyData> itemToValidate,
            string remoteDependencyNameExpected,
            string commandNameExpected,
            TimeSpan accessTimeMax,
            bool successFlagExpected,
            string sqlErrorCodeExpected,
            string sqlErrorMessageExpected)
        {
            // For http name is validated in test itself
            Assert.IsTrue(itemToValidate.Data.BaseData.Name.Contains(remoteDependencyNameExpected),
                "The remote dependancy name is incorrect. Expected: " + remoteDependencyNameExpected +
                ". Collected: " + itemToValidate.Data.BaseData.Name);

            Assert.AreEqual(sqlErrorCodeExpected, itemToValidate.Data.BaseData.ResultCode);

            //If the command name is expected to be empty, the deserializer will make the CommandName null
            if (DependencySourceType.Apmc == DeploymentAndValidationTools.ExpectedSource)
            {
                // Additional checks for profiler collection
                if (!string.IsNullOrEmpty(sqlErrorMessageExpected))
                {
                    Assert.AreEqual(sqlErrorMessageExpected, itemToValidate.Data.BaseData.Properties["ErrorMessage"]);
                }

                if (string.IsNullOrEmpty(commandNameExpected))
                {
                    Assert.IsNull(itemToValidate.Data.BaseData.CommandName);
                }
                else
                {
                    Assert.IsTrue(itemToValidate.Data.BaseData.CommandName.Equals(commandNameExpected), "The command name is incorrect");
                }
            }

            DeploymentAndValidationTools.Validate(itemToValidate, remoteDependencyNameExpected, accessTimeMax, successFlagExpected);
        }
    } 
}
