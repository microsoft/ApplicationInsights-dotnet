namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposing TelemetryConfiguration after each test.")]
    [TestClass]
    public sealed class ProfilerSqlProcessingTest
    {
        private const string DatabaseServer = "ourdatabase.database.windows.net";
        private const string DataBaseName = "mydatabase";
        private const string MyStoredProcName = "apm.MyFavouriteStoredProcedure";
        
        private static readonly string ConnectionString = string.Format(CultureInfo.InvariantCulture, "Server={0};DataBase={1};User=myusername;Password=supersecret", DatabaseServer, DataBaseName);
        private static readonly string ExpectedResourceName = DatabaseServer + " | " + DataBaseName + " | " + MyStoredProcName;
        private static readonly string ExpectedData = MyStoredProcName;
        private static readonly string ExpectedTarget = DatabaseServer + " | " + DataBaseName;
        private static readonly string ExpectedType = "SQL";

        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems;
        private ProfilerSqlProcessing sqlProcessingProfiler;        
        
        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>();
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.sqlProcessingProfiler = new ProfilerSqlProcessing(this.configuration, null, new ObjectInstanceBasedOperationHolder());
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.configuration.Dispose();      
        }

        [TestMethod]
        public void OnBeginSavesTelemetryInWeakTable1()
        {
            var command = GetSqlCommandTestForStoredProc();
            this.sqlProcessingProfiler.OnBeginForOneParameter(command);
            DependencyTelemetry operationReturned = this.sqlProcessingProfiler.TelemetryTable.Get(command).Item1;

            Assert.IsNotNull(operationReturned);
            Assert.AreEqual(ExpectedResourceName, operationReturned.Name, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedTarget, operationReturned.Target, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedType, operationReturned.Type, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedData, operationReturned.Data, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void OnBeginSavesTelemetryInWeakTable2()
        {
            var command = GetSqlCommandTestForStoredProc();
            this.sqlProcessingProfiler.OnBeginForTwoParameters(command, null);
            DependencyTelemetry operationReturned = this.sqlProcessingProfiler.TelemetryTable.Get(command).Item1;

            Assert.IsNotNull(operationReturned);
            Assert.AreEqual(ExpectedResourceName, operationReturned.Name, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedTarget, operationReturned.Target, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedType, operationReturned.Type, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedData, operationReturned.Data, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void OnBeginSavesTelemetryInWeakTable3()
        {
            var command = GetSqlCommandTestForStoredProc();
            this.sqlProcessingProfiler.OnBeginForThreeParameters(command, null, null);
            DependencyTelemetry operationReturned = this.sqlProcessingProfiler.TelemetryTable.Get(command).Item1;

            Assert.IsNotNull(operationReturned);
            Assert.AreEqual(ExpectedResourceName, operationReturned.Name, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedTarget, operationReturned.Target, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedType, operationReturned.Type, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedData, operationReturned.Data, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void OnBeginSavesTelemetryInWeakTable4()
        {
            var command = GetSqlCommandTestForStoredProc();
            this.sqlProcessingProfiler.OnBeginForFourParameters(command, null, null, null);
            DependencyTelemetry operationReturned = this.sqlProcessingProfiler.TelemetryTable.Get(command).Item1;

            Assert.IsNotNull(operationReturned);
            Assert.AreEqual(ExpectedResourceName, operationReturned.Name, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedTarget, operationReturned.Target, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedType, operationReturned.Type, true, CultureInfo.InvariantCulture);
            Assert.AreEqual(ExpectedData, operationReturned.Data, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void OnEndSendsCorrectTelemetry1()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = this.sqlProcessingProfiler.OnBeginForOneParameter(command);
            var objectReturned = this.sqlProcessingProfiler.OnEndForOneParameter(context, returnObjectPassed, command);
            stopwatch.Stop();

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");

            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                expectedName: GetResourceNameForStoredProcedure(command),
                expectedType: RemoteDependencyKind.SQL,
                expectedSuccess: true,
                expectedResultCode: "0",
                timeBetweenBeginEndInMs: stopwatch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void OnEndSendsCorrectTelemetry2()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = this.sqlProcessingProfiler.OnBeginForTwoParameters(command, null);
            var objectReturned = this.sqlProcessingProfiler.OnEndForTwoParameters(context, returnObjectPassed, command, null);
            stopwatch.Stop();

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");

            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                expectedName: GetResourceNameForStoredProcedure(command),
                expectedType: RemoteDependencyKind.SQL,
                expectedSuccess: true,
                expectedResultCode: "0",
                timeBetweenBeginEndInMs: stopwatch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void OnEndSendsCorrectTelemetry3()
        {
            var command = GetSqlCommandTestForStoredProc();
            var returnObjectPassed = new object();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = this.sqlProcessingProfiler.OnBeginForThreeParameters(command, null, null);
            var objectReturned = this.sqlProcessingProfiler.OnEndForThreeParameters(context, returnObjectPassed, command, null, null);
            stopwatch.Stop();

            Assert.AreSame(returnObjectPassed, objectReturned, "Object returned is not the same as expected return object");
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");

            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                expectedName: GetResourceNameForStoredProcedure(command),
                expectedType: RemoteDependencyKind.SQL,
                expectedSuccess: true,
                expectedResultCode: "0",
                timeBetweenBeginEndInMs: stopwatch.ElapsedMilliseconds);
        }
        
        [TestMethod]
        public void OnExceptionSendsCorrectTelemetry1()
        {
            var command = GetSqlCommandTestForStoredProc();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = this.sqlProcessingProfiler.OnBeginForOneParameter(command);
            this.sqlProcessingProfiler.OnExceptionForOneParameter(
                context, 
                TestUtils.GenerateSqlException(10), 
                command);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                expectedName: GetResourceNameForStoredProcedure(command),
                expectedType: RemoteDependencyKind.SQL,
                expectedSuccess: false,
                expectedResultCode: "10",
                timeBetweenBeginEndInMs: stopwatch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void OnExceptionSendsCorrectTelemetry2()
        {
            var command = GetSqlCommandTestForStoredProc();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = this.sqlProcessingProfiler.OnBeginForTwoParameters(command, null);
            this.sqlProcessingProfiler.OnExceptionForTwoParameters(
                context,
                TestUtils.GenerateSqlException(10),
                command,
                null);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                expectedName: GetResourceNameForStoredProcedure(command),
                expectedType: RemoteDependencyKind.SQL,
                expectedSuccess: false,
                expectedResultCode: "10",
                timeBetweenBeginEndInMs: stopwatch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void OnExceptionSendsCorrectTelemetry3()
        {
            var command = GetSqlCommandTestForStoredProc();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var context = this.sqlProcessingProfiler.OnBeginForOneParameter(command);
            this.sqlProcessingProfiler.OnExceptionForThreeParameters(
                context,
                TestUtils.GenerateSqlException(10),
                command,
                null,
                null);
            stopwatch.Stop();

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                expectedName: GetResourceNameForStoredProcedure(command),
                expectedType: RemoteDependencyKind.SQL,
                expectedSuccess: false,
                expectedResultCode: "10",
                timeBetweenBeginEndInMs: stopwatch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void ResourceNameForNonStoredProcIsCollectedCorrectly()
        {
            SqlCommand command = GetSqlCommandTestForQuery();
            string actualResourceName = this.sqlProcessingProfiler.GetResourceName(command);
            Assert.AreEqual(ExpectedTarget, actualResourceName);
        }

        [TestMethod]
        public void CommandNameForNonStoredProcIsCollectedCorrectly()
        {
            SqlCommand command = GetMoreComplexSqlCommandTestForQuery();
            string actualCommandName = this.sqlProcessingProfiler.GetCommandName(command);
            Assert.AreEqual(command.CommandText, actualCommandName);
        }

        [TestMethod]
        public void GetCommandNameReturnsEmptyStringForNullSqlCommand()
        {
            var actualCommandName = this.sqlProcessingProfiler.GetCommandName(null);
            Assert.AreEqual(string.Empty, actualCommandName);
        }

        [TestMethod]
        public void OnBeginLogsWarningWhenFailed()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                try
                {
                    this.sqlProcessingProfiler.OnBeginForOneParameter(null);
                }
                catch (Exception)
                {
                    Assert.Fail("Should not be throwing unhandled exceptions.");
                }

                TestUtils.ValidateEventLogMessage(listener, "will not run for id", EventLevel.Warning);
            }
        }

        [TestMethod]
        public void OnBeginLogsWarningWhenPassedThisObjectWithNullConnection()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                try
                {
                    SqlCommand command = new SqlCommand();
                    this.sqlProcessingProfiler.OnBeginForOneParameter(command);
                }
                catch (Exception)
                {
                    Assert.Fail("Should not be throwing unhandled exceptions");
                }

                var message = listener.Messages.First(item => item.EventId == 14);
                Assert.IsNotNull(message);
            }
        }

        [TestMethod]
        public void OnEndLogsWarningWhenNullObjectPassedToEndMethods()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                try
                {                    
                    this.sqlProcessingProfiler.OnEndForOneParameter(new object(), new object(), null);
                }
                catch (Exception)
                {
                    Assert.Fail("Should not be throwing unhandled exceptions.");
                }

                var message = listener.Messages.First(item => item.EventId == 14);
                Assert.IsNotNull(message);
            }
        }

        [TestMethod]
        public void OnEndLogsWarningWhenObjectPassedToEndWithoutCorrespondingBeginAsync()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Warning, (EventKeywords)AllKeyword);
                try
                {
                    this.sqlProcessingProfiler.OnEndForOneParameter(new object(), new object(), new SqlCommand());
                }
                catch (Exception)
                {
                    Assert.Fail("Should not be throwing unhandled exceptions.");
                }

                var message = listener.Messages.First(item => item.EventId == 12);
                Assert.IsNotNull(message);
            }
        }

        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, 
            string expectedName, 
            RemoteDependencyKind expectedType, 
            bool expectedSuccess,
            string expectedResultCode,
            double timeBetweenBeginEndInMs)
        {            
            Assert.AreEqual(expectedName, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            Assert.AreEqual(expectedType.ToString(), remoteDependencyTelemetryActual.Type, "DependencyKind in the sent telemetry is wrong");
            Assert.AreEqual(expectedSuccess, remoteDependencyTelemetryActual.Success, "Success in the sent telemetry is wrong");
            Assert.AreEqual(expectedResultCode, remoteDependencyTelemetryActual.ResultCode, "ResultCode in the sent telemetry is wrong");

            Assert.IsTrue(remoteDependencyTelemetryActual.Duration.TotalMilliseconds <= timeBetweenBeginEndInMs + 1, "Incorrect duration. Collected " + remoteDependencyTelemetryActual.Duration.TotalMilliseconds + " should be less than max " + timeBetweenBeginEndInMs);
            Assert.IsTrue(remoteDependencyTelemetryActual.Duration.TotalMilliseconds >= 0);

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
    }
}
