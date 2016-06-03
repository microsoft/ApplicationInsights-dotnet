namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ProfilerSqlProcessingTest : IDisposable
    {
        private const string DatabaseServer = "ourdatabase.database.windows.net";
        private const string DataBaseName = "mydatabase";
        private const int TimeAccuracyMilliseconds = 50;
        private const string MyStoredProcName = "apm.MyFavouriteStoredProcedure";
        private const int SleepTimeMsecBetweenBeginAndEnd = 100;
        private static readonly string ConnectionString = string.Format(CultureInfo.InvariantCulture, "Server={0};DataBase={1};User=myusername;Password=supersecret", DatabaseServer, DataBaseName);
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems;
        private Exception ex;
        private ProfilerSqlProcessing sqlProcessingProfiler;        
        
        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.sqlProcessingProfiler = new ProfilerSqlProcessing(this.configuration, null, new ObjectInstanceBasedOperationHolder());
            this.ex = new Exception();
        }

        [TestCleanup]
        public void Cleanup()
        {        
        }

        #region ExecuteReader

        /// <summary>
        /// Validates SQLProcessingProfiler returns correct operation for OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler returns correct operation for OnBeginForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void OnBeginSavesTelemetryInWeakTable()
        {
            var command = GetSqlCommandTestForStoredProc();
            this.sqlProcessingProfiler.OnBeginForExecuteReader(command, null, null);
            DependencyTelemetry operationReturned = this.sqlProcessingProfiler.TelemetryTable.Get(command).Item1;

            var expectedResourceName = GetResourceNameForStoredProcedure(command);
            ValidateDependencyCallOperation(operationReturned, expectedResourceName, RemoteDependencyKind.SQL, "OnBeginForExecuteReader");            
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnEndForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler sends correct telemetry on calling OnEndForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnEndForExecuteReader()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            var context = this.sqlProcessingProfiler.OnBeginForExecuteReader(command, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            var objectReturned = this.sqlProcessingProfiler.OnEndForExecuteReader(context, returnObjectPassed, command, null, null);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForExecuteReader processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                true,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnExceptionForExecuteReader()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            var context = this.sqlProcessingProfiler.OnBeginForExecuteReader(command, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            this.sqlProcessingProfiler.OnExceptionForExecuteReader(
                context, 
                TestUtils.GenerateSqlException(10), 
                command, 
                null, 
                null);
            
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                false,
                SleepTimeMsecBetweenBeginAndEnd, 
                "10");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnBeginForExecuteReaderFailed()
        {
            using (var listener = new TestEventListener())
            {
            const long AllKeyword = -1;
            listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
            try
            {
                SqlCommand command = null;                
                DependencyTelemetry operationReturned = (DependencyTelemetry)this.sqlProcessingProfiler.OnBeginForExecuteReader(command, null, null);
            }
            catch (Exception)
            {
                Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");                
            }

            TestUtils.ValidateEventLogMessage(listener, "will not run for id", EventLevel.Warning);
            }
        }       

        #endregion //ExecuteReader

        #region SynCallBacks

        /// <summary>
        /// Validates SQLProcessingProfiler returns correct operation for OnBeginForSync.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler returns correct operation for OnBeginForSync.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnBeginForSynCallbacks()
        {
            var command = GetSqlCommandTestForStoredProc();
            this.sqlProcessingProfiler.OnBeginForSync(command);
            DependencyTelemetry operationReturned = this.sqlProcessingProfiler.TelemetryTable.Get(command).Item1;
            var expectedResourceName = GetResourceNameForStoredProcedure(command);
            ValidateDependencyCallOperation(operationReturned, expectedResourceName, RemoteDependencyKind.SQL, "OnBeginForSync");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnEndForSync.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler sends correct telemetry on calling OnEndForSync.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnEndForSynCallbacks()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            var context = this.sqlProcessingProfiler.OnBeginForSync(command);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            var objectReturned = this.sqlProcessingProfiler.OnEndForSync(context, returnObjectPassed, command);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForSync processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                true,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForSync.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForSync.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnExceptionForSynCallbacks()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            var context = this.sqlProcessingProfiler.OnBeginForSync(command);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            this.sqlProcessingProfiler.OnExceptionForSync(context, this.ex, command);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                false,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }
        #endregion //SynCallBacks

        #region BeginExecuteNonQueryInternal-End

        /// <summary>
        /// Validates SQLProcessingProfiler returns correct operation for OnBeginForBeginExecuteNonQueryInternal.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler returns correct operation for OnBeginForBeginExecuteNonQueryInternal.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnBeginForBeginExecuteNonQueryInternal()
        {
            var command = GetSqlCommandTestForStoredProc();
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.sqlProcessingProfiler.OnBeginForBeginExecuteNonQueryInternal(command, null, null, null, null);
            var expectedResourceName = GetResourceNameForStoredProcedure(command);
            Assert.IsNull(operationReturned, "Operation returned should be null as all context is maintained internally for Async calls");            
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnEndForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler sends correct telemetry on calling OnEndForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginAndEndExecuteNonQueryInternal()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            this.sqlProcessingProfiler.OnBeginForBeginExecuteNonQueryInternal(command, null, null, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            var objectReturned = this.sqlProcessingProfiler.OnEndForSqlAsync(null, returnObjectPassed, command, null);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForSqlAsync processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                true,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginAndEndExecuteNonQueryInternalException()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            this.sqlProcessingProfiler.OnBeginForBeginExecuteNonQueryInternal(command, null, null, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            this.sqlProcessingProfiler.OnExceptionForSqlAsync(null, this.ex, command, null);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                false,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginExecuteNonQueryInternalFailedOnStart()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    this.sqlProcessingProfiler.OnBeginForBeginExecuteNonQueryInternal(null, null, null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "will not run for id", EventLevel.Warning);
            }
        }
       
        #endregion //BeginExecuteNonQueryInternal-End

        #region BeginExecuteReaderInternal-End

        /// <summary>
        /// Validates SQLProcessingProfiler returns correct operation for OnBeginForBeginExecuteReaderInternal.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler returns correct operation for OnBeginForBeginExecuteReaderInternal.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnBeginForBeginExecuteReaderInternal()
        {
            var command = GetSqlCommandTestForStoredProc();
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.sqlProcessingProfiler.OnBeginForBeginExecuteReaderInternal(command, null, null, null, null, null);
            var expectedResourceName = GetResourceNameForStoredProcedure(command);
            Assert.IsNull(operationReturned, "Operation returned should be null as all context is maintained internally for Async calls");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnBeginForBeginExecuteReaderInternal.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler sends correct telemetry on calling OnBeginForBeginExecuteReaderInternal.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginAndForBeginExecuteReaderInternal()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            this.sqlProcessingProfiler.OnBeginForBeginExecuteReaderInternal(command, null, null, null, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            var objectReturned = this.sqlProcessingProfiler.OnEndForSqlAsync(null, returnObjectPassed, command, null);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForSqlAsync processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                true,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginAndForBeginExecuteReaderInternalException()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            this.sqlProcessingProfiler.OnBeginForBeginExecuteReaderInternal(command, null, null, null, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            this.sqlProcessingProfiler.OnExceptionForSqlAsync(null, this.ex, command, null);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                false,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginExecuteReaderInternalFailedOnStart()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    this.sqlProcessingProfiler.OnBeginForBeginExecuteReaderInternal(null, null, null, null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "will not run for id", EventLevel.Warning);                 
            }
        }
        #endregion //BeginExecuteReaderInternal-End

        #region BeginExecuteXmlReader-End
        /// <summary>
        /// Validates SQLProcessingProfiler returns correct operation for OnBeginForBeginExecuteXmlReaderInternal.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler returns correct operation for OnBeginForBeginExecuteXmlReaderInternal.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerOnBeginForBeginExecuteXmlReaderInternal()
        {
            var command = GetSqlCommandTestForStoredProc();
            DependencyTelemetry operationReturned = (DependencyTelemetry)this.sqlProcessingProfiler.OnBeginForBeginExecuteXmlReaderInternal(command, null, null, null, null);
            var expectedResourceName = GetResourceNameForStoredProcedure(command);
            Assert.IsNull(operationReturned, "Operation returned should be null as all context is maintained internally for Async calls");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnBeginForBeginExecuteXmlReaderInternal.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler sends correct telemetry on calling OnBeginForBeginExecuteXmlReaderInternal.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginAndForBeginExecuteXmlReaderInternal()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            this.sqlProcessingProfiler.OnBeginForBeginExecuteXmlReaderInternal(command, null, null, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            var objectReturned = this.sqlProcessingProfiler.OnEndForSqlAsync(null, returnObjectPassed, command, null);

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForSqlAsync processor is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                true,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler sends correct telemetry on calling OnBeginForBeginExecuteXmlReaderInternal.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler sends correct telemetry on calling OnExceptionForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginAndForBeginExecuteXmlReaderInternalException()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();
            this.sqlProcessingProfiler.OnBeginForBeginExecuteXmlReaderInternal(command, null, null, null, null);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);
            this.sqlProcessingProfiler.OnExceptionForSqlAsync(null, this.ex, command, null);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                GetResourceNameForStoredProcedure(command),
                RemoteDependencyKind.SQL,
                false,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerBeginExecuteXmlReaderInternalFailedOnStart()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    this.sqlProcessingProfiler.OnBeginForBeginExecuteXmlReaderInternal(null, null, null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "will not run for id", EventLevel.Warning);
            }
        }
        #endregion //BeginExecuteXmlReader-End

        #region SyncScenarios

        #endregion //SyncScenarios

        #region AsyncScenarios

        #endregion AsyncScenarios

        #region Misc

        /// <summary>
        /// Validates SQLProcessingProfiler determines resource name correctly for stored procedure.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler determines resource name correctly for stored procedure.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerResourceNameTestForStoredProc()
        {
            var command = GetSqlCommandTestForStoredProc();
            var expectedName = DatabaseServer + " | " + DataBaseName + " | " + MyStoredProcName;
            var actualResourceName = this.sqlProcessingProfiler.GetResourceName(command);                        
            Assert.AreEqual(expectedName, actualResourceName, "SqlProcessingProfiler returned incorrect resource name");
            
            Assert.AreEqual(string.Empty, this.sqlProcessingProfiler.GetResourceName(null), "SqlProcessingProfiler should return String.Empty for null object");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler determines resource name correctly for non stored procedure.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler determines resource name correctly for non stored procedure.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerResourceNameTestForNonStoredProc()
        {
            var command = GetSqlCommandTestForQuery();
            var expectedName = GetResourceNameForQuery(command);
            var actualResourceName = this.sqlProcessingProfiler.GetResourceName(command);
            Assert.AreEqual(expectedName, actualResourceName, "SqlProcessingProfiler returned incorrect resource name");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler determines command name correctly for non stored procedure.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler determines command name correctly for non stored procedure.")]
        [Owner("arthurbe")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerCommandNameTestForNonStoredProc()
        {
            var command = GetMoreComplexSqlCommandTestForQuery();
            var actualCommandName = this.sqlProcessingProfiler.GetCommandName(command);
            Assert.AreEqual(command.CommandText, actualCommandName, "SqlProcessingProfiler returned an incorrect command name");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler correctly returns empty string for a null connection object.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler handles a null command object.")]
        [Owner("arthurbe")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerCommandNameTestForNullCommand()
        {
            var actualCommandName = this.sqlProcessingProfiler.GetCommandName(null);
            Assert.AreEqual(string.Empty, actualCommandName, "SqlProcessingProfiler should return empty string for null command object");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler correctly returns empty string for a null command object.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler handles a null connection object.")]
        [Owner("arthurbe")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerCommandNameTestForNullConnection()
        {
            var command = GetMoreComplexSqlCommandTestForQuery();
            command.Connection = null;
            var actualCommandName = this.sqlProcessingProfiler.GetCommandName(command);
            Assert.AreEqual(string.Empty, actualCommandName, "SqlProcessingProfiler should return empty string for null connection object");
        }

        /// <summary>
        /// Validates SQLProcessingProfiler returns stored procedure name in the case of a stored procedure.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingProfiler returns stored procedure name in the case of a stored procedure.")]
        [Owner("arthurbe")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerReturnsProcNameForStoredProc()
        {
            var command = GetSqlCommandTestForStoredProc();
            Assert.AreEqual(command.CommandText, this.sqlProcessingProfiler.GetCommandName(command), "SqlProcessingProfiler should return the stored procedure name in the case of a StoredProc");
        }

        #endregion //Misc

        #region LoggingTests
        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject with null connection.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler logs error into EventLog when passed invalid thisObject with null connection")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerLogsResourceNameNull()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                try
                {
                    SqlCommand command = new SqlCommand();
                    this.sqlProcessingProfiler.OnBeginForBeginExecuteXmlReaderInternal(command, null, null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");
                }

                var message = listener.Messages.First(item => item.EventId == 14);
                Assert.IsNotNull(message);
            }
        }

        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler logs error into EventLog when passed invalid thisObject to any OnEnd.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerLogsWhenNullObjectPassedToEndMethods()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                try
                {                    
                    var objectReturned = this.sqlProcessingProfiler.OnEndForSqlAsync(new object(), null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");
                }

                var message = listener.Messages.First(item => item.EventId == 14);
                Assert.IsNotNull(message);
            }
        }

        /// <summary>
        /// Validates SQLProcessingProfiler logs error into EventLog when passed an object to End without corresponding begin in async pattern.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingProfiler logs error into EventLog when passed an object to End without corresponding begin in async pattern")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingProfilerLogsWhenObjectPassedToEndWithoutCorrespondingBeginAsync()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    var objectReturned = this.sqlProcessingProfiler.OnEndForSqlAsync(new object(), null, new SqlCommand(), null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingProfiler should not be throwing unhandled exceptions");
                }

                var message = listener.Messages.First(item => item.EventId == 12);
                Assert.IsNotNull(message);
            }
        }
        #endregion //LoggingTests

        #region Disposable
        public void Dispose()
        {
            this.configuration.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Disposable

        #region Helpers
        private static void ValidateDependencyCallOperation(DependencyTelemetry operation, string name, RemoteDependencyKind type, string methodName)
        {
            Assert.IsNotNull(operation, "Operation returned should not be null for method:" + methodName);
            Assert.AreEqual(name, operation.Name, true, "Resource name in the returned operation is wrong for method:" + methodName);            
        }

        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, string name, RemoteDependencyKind kind, bool success, double valueMin, string resultCode)
        {            
            Assert.AreEqual(name, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            Assert.AreEqual(kind.ToString(), remoteDependencyTelemetryActual.DependencyKind, "DependencyKind in the sent telemetry is wrong");
            Assert.AreEqual(success, remoteDependencyTelemetryActual.Success, "Success in the sent telemetry is wrong");
            Assert.AreEqual(resultCode, remoteDependencyTelemetryActual.ResultCode, "ResultCode in the sent telemetry is wrong");

            var valueMinRelaxed = valueMin - 50;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration >= TimeSpan.FromMilliseconds(valueMinRelaxed),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should be equal or more than the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            var valueMax = valueMin + TimeAccuracyMilliseconds;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration <= TimeSpan.FromMilliseconds(valueMax),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should not be significantly bigger than the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModuleTest), prefix: "rddp:");
            Assert.AreEqual(expectedVersion, remoteDependencyTelemetryActual.Context.GetInternalContext().SdkVersion);
        }

        private static SqlCommand GetSqlCommandTestForQuery()
        {
            var con = new SqlConnection(ConnectionString);
            SqlCommand command = con.CreateCommand();
            command.CommandText = "Select * from APM.Database";
            return command;
        }

        private static SqlCommand GetMoreComplexSqlCommandTestForQuery()
        {
            var con = new SqlConnection(ConnectionString);
            SqlCommand command = con.CreateCommand();
            command.CommandText = "Select f.FailedTest,c.CustID from Customers c inner join Failures f on f.custID = c.CustID where f.Failures > @Param1 and c.CustID=@CustomerID by f.custID";
            return command;
        }

        private static string GetResourceNameForQuery(SqlCommand command)
        {
            return string.Join(
                " | ",
                command.Connection.DataSource,
                command.Connection.Database);
        }

        private static SqlCommand GetSqlCommandTestForStoredProc()
        {
            var con = new SqlConnection(ConnectionString);
            SqlCommand command = con.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = MyStoredProcName;
            return command;
        }

        private static string GetResourceNameForStoredProcedure(SqlCommand command)
        {
            return string.Join(
                " | ",
                command.Connection.DataSource,
                command.Connection.Database,
                command.CommandText);
        }
        #endregion Helpers
    }
}
