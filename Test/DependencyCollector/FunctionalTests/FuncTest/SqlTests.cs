namespace FuncTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using FuncTest.Helpers;
    using FuncTest.Serialization;
    

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
        /// Resource Name for wrong database.
        /// </summary>
        private const string ResourceNameSQLToWrongDatabase = @"invalid\SQLEXPRESS | RDDTestDatabase";

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

        #region Misc tests
        [TestMethod]
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        public void TestRddForSyncSqlAspx451()
        {
            if (!RegistryCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.ExecuteSyncSqlTests(DeploymentAndValidationTools.Aspx451TestWebApplication, 1, 1, AccessTimeMaxSqlCallToApmdbNormal);
        }

        [TestCategory(TestCategory.Net451)]
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
                         var sqlItems = allItems.Where(i => i.data.baseData.type == "SQL").ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");
                         this.Validate(
                             sqlItems[0],
                             ResourceNameSQLToDevApm,
                             StoredProcedureName, 
                             TimeSpan.FromSeconds(10), 
                             successFlagExpected: true,
                             sqlErrorCodeExpected: "0",
                             sqlErrorMessageExpected: null);
                     });           
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequence()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderTwiceInSequenceFailed()
        {
            this.TestSqlCommandExecute("TestExecuteReaderTwiceInSequence", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
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
                         var sqlItems = allItems.Where(i => i.data.baseData.type == "SQL").ToArray();
                         Assert.AreEqual(1, sqlItems.Length, "We should only report 1 dependency call");
                     });
        }
        #endregion

        #region ExecuteReader

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteReaderAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReader()
        {
            this.TestSqlCommandExecute("BeginExecuteReader1", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed_0Args()
        {
            this.TestSqlCommandExecute("BeginExecuteReader0", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed_1Arg()
        {
            this.TestSqlCommandExecute("BeginExecuteReader1", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed_2Arg()
        {
            this.TestSqlCommandExecute("BeginExecuteReader2", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteReaderFailed_3Arg()
        {
            this.TestSqlCommandExecute("BeginExecuteReader3", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader1", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReaderFailed_0Args()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader0", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteReaderFailed_1Args()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteReader1", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }
        #endregion

        #region ExecuteScalar
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsync()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteScalarAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteScalarAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalar()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteScalarFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteScalar", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }
        #endregion

        #region ExecuteNonQuery

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQuery()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteNonQuery", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteNonQueryAsync()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestCategory(TestCategory.Net451)]
        [TestMethod]
        public void TestExecuteNonQueryAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteNonQueryAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestCategory(TestCategory.Net451)]
        [TestMethod]
        public void TestBeginExecuteNonQuery_Arg0()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery0", errorNumber: "0", errorMessage: null);
        }

        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestCategory(TestCategory.Net451)]
        [TestMethod]
        public void TestBeginExecuteNonQuery_Arg2()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery2", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteNonQueryFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteNonQuery0", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }
        #endregion

        #region ExecuteXmlReader
        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsync()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestExecuteXmlReaderAsyncFailed()
        {
            this.TestSqlCommandExecute("ExecuteXmlReaderAsync", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.", extraClauseForFailureCase: ForXMLClauseInFailureCase);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestBeginExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("BeginExecuteXmlReader", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReader()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", errorNumber: "0", errorMessage: null);
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlCommandExecuteXmlReaderFailed()
        {
            this.TestSqlCommandExecute("SqlCommandExecuteXmlReader", errorNumber: "208", errorMessage: "Invalid object name 'apm.Database1212121'.");
        }
        #endregion

        #region SqlConnection.Open

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenSuccess()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpen", true, "", false);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAccountException()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpen", false, "account", false);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenServerException()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpen", false, "server", false);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAsyncSuccess()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpenAsync", true, "", true);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAsyncAccountException()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpenAsync", false, "account", true);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAsyncServerException()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpenAsync", false, "server", true);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAsyncAwaitSuccess()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpenAsyncAwait", true, "", false);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAsyncAwaitAccountException()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpenAsyncAwait", false, "account", false);
            }
        }

        [TestCategory(TestCategory.Net451)]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", DeploymentAndValidationTools.Aspx451AppFolder)]
        [TestMethod]
        public void TestSqlConnectionOpenAsyncAwaitServerException()
        {
            // instrumentation works only in case of profiler
            if (RegistryCheck.IsStatusMonitorInstalled)
            {
                this.TestSqlConnectionExecute("SqlConnectionOpenAsyncAwait", false, "server", false);
            }
        }

        #endregion

        private void TestSqlConnectionExecute(string type, bool success, string exceptionType, bool async)
        {
            DeploymentAndValidationTools.Aspx451TestWebApplication.DoTest(
                 application =>
                 {
                     string responseForQueryValidation = application.ExecuteAnonymousRequest("?type=" + type + "&success=" + success + "&exceptionType=" + exceptionType);

                     //// The above request would have trigged RDD module to monitor and create RDD telemetry
                     //// Listen in the fake endpoint and see if the RDDTelemtry is captured                      

                     var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(
                         async ? DeploymentAndValidationTools.SleepTimeForSdkToSendAsyncEvents : DeploymentAndValidationTools.SleepTimeForSdkToSendEvents);
                     var sqlItems = allItems.Where(i => i.data.baseData.type == "SQL").ToArray();
                     if (success)
                     {
                         // no items should be sent in case of success connection to database
                         Assert.AreEqual(0, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");
                     }
                     else
                     { 
                         Assert.AreEqual(1, sqlItems.Length, "Total Count of Remote Dependency items for SQL collected is wrong.");

                         bool isAccountException = exceptionType.Equals("account", StringComparison.OrdinalIgnoreCase);
                         this.Validate(
                             sqlItems[0],
                             isAccountException ? ResourceNameSQLToDevApm : ResourceNameSQLToWrongDatabase,
                             "Open",
                             TimeSpan.FromSeconds(20),
                             successFlagExpected: success,
                             sqlErrorCodesExpected: isAccountException ? new List<string> { "233", "18456" } : new List<string> { "-1" },
                             sqlErrorMessagesExpected: isAccountException
                                ? new List<string> { "A connection was successfully established with the server, but then an error occurred during the login process.", "Login failed for user 'AiUser'." }
                                : new List<string> { "A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible." });
                     }
                 });
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
                     var sqlItems = allItems.Where(i => i.data.baseData.type == "SQL").ToArray();                     
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

                    var allItems = DeploymentAndValidationTools.SdkEventListener.ReceiveAllItemsDuringTimeOfType<TelemetryItem<RemoteDependencyData>>(DeploymentAndValidationTools.SleepTimeForSdkToSendEvents).ToArray();
                    var sqlItems = allItems.Where(i => i.data.baseData.type == "SQL").ToArray();


                    Assert.AreEqual(
                        expectedCount,
                        sqlItems.Length,
                        "Total Count of Remote Dependency items for SQL collected is wrong.");

                    foreach (var sqlItem in sqlItems)
                    {
                        string spName = "GetTopTenMessages";
                        this.Validate(
                            sqlItem, 
                            ResourceNameSQLToDevApm, 
                            spName, 
                            accessTimeMax, successFlagExpected: true,
                            sqlErrorCodeExpected: "0",
                            sqlErrorMessageExpected: null);
                    }
                });
        }

        private void Validate(TelemetryItem<RemoteDependencyData> itemToValidate,
            string targetExpected,
            string commandNameExpected,
            TimeSpan accessTimeMax,
            bool successFlagExpected,
            string sqlErrorCodeExpected,
            string sqlErrorMessageExpected)
        {
            // For http name is validated in test itself
            Assert.IsTrue(itemToValidate.data.baseData.target.Contains(targetExpected),
                "The remote dependancy target is incorrect. Expected: " + targetExpected +
                ". Collected: " + itemToValidate.data.baseData.target);

            Assert.AreEqual(sqlErrorCodeExpected, itemToValidate.data.baseData.resultCode);

            //If the command name is expected to be empty, the deserializer will make the CommandName null
            if ("rddp" == DeploymentAndValidationTools.ExpectedSDKPrefix)
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
                    Assert.IsTrue(itemToValidate.data.baseData.data.Equals(commandNameExpected), "The command name is incorrect");
                }
            }

            DeploymentAndValidationTools.Validate(itemToValidate, accessTimeMax, successFlagExpected, sqlErrorCodeExpected);
        }

        private void Validate(TelemetryItem<RemoteDependencyData> itemToValidate,
           string targetExpected,
           string commandNameExpected,
           TimeSpan accessTimeMax,
           bool successFlagExpected,
           List<string> sqlErrorCodesExpected,
           List<string> sqlErrorMessagesExpected)
        {
            // For http name is validated in test itself
            Assert.IsTrue(itemToValidate.data.baseData.target.Contains(targetExpected),
                "The remote dependancy target is incorrect. Expected: " + targetExpected +
                ". Collected: " + itemToValidate.data.baseData.target);

            Assert.IsTrue(sqlErrorCodesExpected.Contains(itemToValidate.data.baseData.resultCode));

            //If the command name is expected to be empty, the deserializer will make the CommandName null
            if ("rddp" == DeploymentAndValidationTools.ExpectedSDKPrefix)
            {
                // Additional checks for profiler collection
                if (sqlErrorMessagesExpected != null)
                {
                    bool found = false;
                    var errorMessage = itemToValidate.data.baseData.properties["ErrorMessage"];
                    foreach (var message in sqlErrorMessagesExpected)
                    {
                        if (errorMessage.Contains(message))
                        {
                            found = true;
                            break;
                        }
                    }

                    Assert.IsTrue(found, "The error message is incorrect");
                }

                if (string.IsNullOrEmpty(commandNameExpected))
                {
                    Assert.IsNull(itemToValidate.data.baseData.data);
                }
                else
                {
                    Assert.IsTrue(itemToValidate.data.baseData.data.Equals(commandNameExpected), "The command name is incorrect");
                }
            }

            DeploymentAndValidationTools.Validate(itemToValidate, accessTimeMax, successFlagExpected);
        }
    } 
}