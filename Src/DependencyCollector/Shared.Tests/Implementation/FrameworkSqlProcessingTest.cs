#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class FrameworkSqlProcessingTest : IDisposable
    {
        private const int TimeAccuracyMilliseconds = 50;
        private const int SleepTimeMsecBetweenBeginAndEnd = 100;
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems;
        private FrameworkSqlProcessing sqlProcessingFramework;
        private string resourceName = "ourdatabase.database.windows.net | mydatabase";

        [TestInitialize]
        public void TestInitialize()
        {
             this.configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>(); 
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.sqlProcessingFramework = new FrameworkSqlProcessing(this.configuration, new CacheBasedOperationHolder());
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        #region ExecuteReader

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetrySqlQuerySucess()
        {
            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111, 
                database: "mydatabase",
                dataSource: "ourdatabase.database.windows.net",
                commandText: string.Empty);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: true, synchronous: true, sqlExceptionNumber: 0);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                this.resourceName,
                RemoteDependencyKind.SQL,
                true,
                false,
                1,
                SleepTimeMsecBetweenBeginAndEnd,
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetrySqlQueryAsync()
        {
            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111,
                database: "mydatabase",
                dataSource: "ourdatabase.database.windows.net",
                commandText: string.Empty);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: true, synchronous: false, sqlExceptionNumber: 0);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                this.resourceName,
                RemoteDependencyKind.SQL,
                true,
                true,
                1,
                SleepTimeMsecBetweenBeginAndEnd,
                "0");
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in failed call.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingFramework sends correct telemetry for non stored procedure in failed call.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetrySqlQueryFailed()
        {
            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111,
                database: "mydatabase",
                dataSource: "ourdatabase.database.windows.net",
                commandText: string.Empty);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: false, synchronous: true, sqlExceptionNumber: 1);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                this.resourceName,
                RemoteDependencyKind.SQL,
                false,
                false,
                1,
                SleepTimeMsecBetweenBeginAndEnd,
                "1");
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for stored procedure.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetryStoredProc()
        {
            string resourceNameSproc = "ourdatabase.database.windows.net | mydatabase | apm.MyFavouriteStoredProcedure";

            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111, 
                dataSource: "ourdatabase.database.windows.net", 
                database: "mydatabase", 
                commandText: "apm.MyFavouriteStoredProcedure");
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(
                id: 1111,
                success: true,
                synchronous: true, 
                sqlExceptionNumber: 0);

            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                resourceNameSproc,
                RemoteDependencyKind.SQL,
                true,
                false,
                1,
                SleepTimeMsecBetweenBeginAndEnd, 
                "0");
        }
        #endregion

        /*
        #region LoggingTests
        /// <summary>
        /// Validates sqlProcessingFramework logs error into EventLog when passed invalid thisObject with null connection.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingFramework logs error into EventLog when passed invalid thisObject with null connection")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkLogsResourceNameNull()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    SqlCommand command = new SqlCommand();
                    this.sqlProcessingFramework.OnBeginForBeginExecuteXmlReaderInternal(command, null, null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingFramework should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "sqlProcessingFramework is dropping item as resource name");                                 
            }
        }

        /// <summary>
        /// Validates sqlProcessingFramework logs error into EventLog when passed invalid thisObject to OnBeginForExecuteReader.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingFramework logs error into EventLog when passed invalid thisObject to any OnEnd.")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkLogsWhenNullObjectPassedToEndMethods()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {                    
                    var objectReturned = this.sqlProcessingFramework.OnEndForSqlAsync(new object(), null, null, null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingFramework should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "OnEndSql failed");                                 
            }
        }

        /// <summary>
        /// Validates sqlProcessingFramework logs error into EventLog when passed an object to End without corresponding begin in async pattern.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingFramework logs error into EventLog when passed an object to End without corresponding begin in async pattern")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkLogsWhenObjectPassedToEndWithoutCorrespondingBeginAsync()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    var objectReturned = this.sqlProcessingFramework.OnEndForSqlAsync(new object(), null, new SqlCommand(), null);
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingFramework should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "sqlProcessingFramework will be dropping item in Async path as corresponding begin not found");                 
            }
        }

        /// <summary>
        /// Validates sqlProcessingFramework does not create RDD packet when passed an object to End without corresponding begin in sync pattern
        /// In sync pattern, the context is not stored in the processor, instead it is expected to be maintained in customer code. This checks that
        /// if an invalid context is passed for some reason, we don't crash and no event is generated.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingFramework does not create RDD packet when passed an object to End without corresponding begin in sync pattern")]
        [Owner("cithomas")]
        [TestCategory("CVT")]
        public void RddTestSqlProcessingFrameworkLogsWhenObjectPassedToEndWithoutCorrespondingBeginSync()
        {
            using (var listener = new TestEventListener())
            {
                const long AllKeyword = -1;
                listener.EnableEvents(DependencyCollectorEventSource.Log, EventLevel.Verbose, (EventKeywords)AllKeyword);
                try
                {
                    var command = GetSqlCommandTestForStoredProc();
                    var returnObjectPassed = new object();
                    var context = this.sqlProcessingFramework.OnBeginForExecuteReader(command, null, null);
                    var contextWithNotMatchingBegin = new DependencyTelemetry();
                    var objectReturned = this.sqlProcessingFramework.OnEndForExecuteReader(contextWithNotMatchingBegin, returnObjectPassed, command, null, null);

                    Assert.AreSame(returnObjectPassed, objectReturned, "Object returned from OnEndForExecuteReader processor is not the same as expected return object");
                    Assert.AreEqual(0, this.sendItems.Count, "No RDD packets should be created for invalid context.");   
                }
                catch (Exception)
                {
                    Assert.Fail("sqlProcessingFramework should not be throwing unhandled exceptions");
                }

                TestUtils.ValidateEventLogMessage(listener, "End operation failed with exception");                                 
            }
        }
        #endregion //LoggingTests
        */

        #region Disposable
        public void Dispose()
        {
            this.configuration.Dispose();
            this.sqlProcessingFramework.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Disposable

        #region Helpers

        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, string name, RemoteDependencyKind kind, bool success, bool async, int count, double valueMin, string errorCode)
        {
            Assert.AreEqual(name, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            Assert.AreEqual(kind.ToString(), remoteDependencyTelemetryActual.DependencyKind, "DependencyKind in the sent telemetry is wrong");
            Assert.AreEqual(success, remoteDependencyTelemetryActual.Success, "Success in the sent telemetry is wrong");
            Assert.AreEqual(errorCode, remoteDependencyTelemetryActual.ResultCode, "ResultCode in the sent telemetry is wrong");

            var valueMinRelaxed = valueMin - 50;
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration >= TimeSpan.FromMilliseconds(valueMinRelaxed),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should be equal or more than the time duration between start and end", remoteDependencyTelemetryActual.Duration));

            var valueMax = valueMin + (valueMin * TimeAccuracyMilliseconds);
            Assert.IsTrue(
                remoteDependencyTelemetryActual.Duration <= TimeSpan.FromMilliseconds(valueMax),
                string.Format(CultureInfo.InvariantCulture, "Value (dependency duration = {0}) in the sent telemetry should not be significantly bigger than the time duration between start and end", remoteDependencyTelemetryActual.Duration));
        }

        #endregion Helpers
    }
}
#endif