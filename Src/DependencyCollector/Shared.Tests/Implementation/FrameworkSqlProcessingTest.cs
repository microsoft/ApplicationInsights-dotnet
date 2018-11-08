namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public sealed class FrameworkSqlProcessingTest : IDisposable
    {
        private const int TimeAccuracyMilliseconds = 50;
        private const int SleepTimeMsecBetweenBeginAndEnd = 100;
        private TelemetryConfiguration configuration;
        private List<ITelemetry> sendItems;
        private FrameworkSqlProcessing sqlProcessingFramework;

        [TestInitialize]
        public void TestInitialize()
        {
            this.configuration = new TelemetryConfiguration();
            this.sendItems = new List<ITelemetry>(); 
            this.configuration.TelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };
            this.configuration.InstrumentationKey = Guid.NewGuid().ToString();
            this.configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            this.sqlProcessingFramework = new FrameworkSqlProcessing(this.configuration, new CacheBasedOperationHolder("testCache", 100 * 1000));
        }

        [TestCleanup]
        public void Cleanup()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

#region ExecuteReader

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetrySqlQuerySucess()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();            

            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111, 
                database: "mydatabase",
                dataSource: "ourdatabase.database.windows.net",
                commandText: string.Empty);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: true, sqlExceptionNumber: 0);
            stopwatch.Stop();

            Assert.IsNull(Activity.Current);
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                "ourdatabase.database.windows.net | mydatabase",
                "ourdatabase.database.windows.net | mydatabase",
                RemoteDependencyConstants.SQL,
                true,
                stopwatch.Elapsed.TotalMilliseconds,
                string.Empty);
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetryMultipleItems()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var parent = new Activity("parent").Start();

            for (int i = 0; i < 10; i++)
            {
                this.sqlProcessingFramework.OnBeginExecuteCallback(
                    id: i,
                    database: "mydatabase",
                    dataSource: "ourdatabase.database.windows.net",
                    commandText: string.Empty);
                Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

                this.sqlProcessingFramework.OnEndExecuteCallback(id: i, success: true, sqlExceptionNumber: 0);
                stopwatch.Stop();

                Assert.AreEqual(parent, Activity.Current);
                Assert.AreEqual(i + 1, this.sendItems.Count, "Only one telemetry item should be sent");

                var dependencyTelemetry = this.sendItems[0] as DependencyTelemetry;
                ValidateTelemetryPacket(
                    dependencyTelemetry,
                    "ourdatabase.database.windows.net | mydatabase",
                    "ourdatabase.database.windows.net | mydatabase",
                    RemoteDependencyConstants.SQL,
                    true,
                    stopwatch.Elapsed.TotalMilliseconds,
                    string.Empty);

                Assert.AreEqual(parent.Id, dependencyTelemetry.Context.Operation.ParentId);
                Assert.AreEqual(parent.RootId, dependencyTelemetry.Context.Operation.Id);
            }
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for non stored procedure in async call.")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetrySqlQueryAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111,
                database: "mydatabase",
                dataSource: "ourdatabase.database.windows.net",
                commandText: string.Empty);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: true, sqlExceptionNumber: 0);
            stopwatch.Stop();
            Assert.IsNull(Activity.Current);
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                "ourdatabase.database.windows.net | mydatabase",
                "ourdatabase.database.windows.net | mydatabase",
                RemoteDependencyConstants.SQL,
                true,
                stopwatch.Elapsed.TotalMilliseconds,
                string.Empty);
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry for non stored procedure in failed call.
        /// </summary>
        [TestMethod]
        [Description("Validates sqlProcessingFramework sends correct telemetry for non stored procedure in failed call.")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetrySqlQueryFailed()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111,
                database: "mydatabase",
                dataSource: "ourdatabase.database.windows.net",
                commandText: string.Empty);
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: false, sqlExceptionNumber: 1);
            stopwatch.Stop();

            Assert.IsNull(Activity.Current);
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                "ourdatabase.database.windows.net | mydatabase",
                "ourdatabase.database.windows.net | mydatabase",
                RemoteDependencyConstants.SQL,
                false,
                stopwatch.Elapsed.TotalMilliseconds,
                "1");
        }

        /// <summary>
        /// Validates SQLProcessingFramework sends correct telemetry.
        /// </summary>
        [TestMethod]
        [Description("Validates SQLProcessingFramework sends correct telemetry for stored procedure.")]
        public void RddTestSqlProcessingFrameworkSendsCorrectTelemetryStoredProc()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.sqlProcessingFramework.OnBeginExecuteCallback(
                id: 1111, 
                dataSource: "ourdatabase.database.windows.net", 
                database: "mydatabase", 
                commandText: "apm.MyFavouriteStoredProcedure");
            Thread.Sleep(SleepTimeMsecBetweenBeginAndEnd);

            this.sqlProcessingFramework.OnEndExecuteCallback(id: 1111, success: true, sqlExceptionNumber: 0);

            stopwatch.Stop();
            Assert.IsNull(Activity.Current);
            Assert.AreEqual(1, this.sendItems.Count, "Only one telemetry item should be sent");
            ValidateTelemetryPacket(
                this.sendItems[0] as DependencyTelemetry,
                "ourdatabase.database.windows.net | mydatabase",
                "apm.MyFavouriteStoredProcedure",
                RemoteDependencyConstants.SQL,
                true,
                stopwatch.Elapsed.TotalMilliseconds, 
                string.Empty);
        }
#endregion

#region Disposable
        public void Dispose()
        {
            this.configuration.Dispose();            
            GC.SuppressFinalize(this);
        }
#endregion Disposable

#region Helpers

        private static void ValidateTelemetryPacket(
            DependencyTelemetry remoteDependencyTelemetryActual, string target, string name, string type, bool success, double valueMin, string errorCode)
        {
            Assert.AreEqual(name, remoteDependencyTelemetryActual.Name, true, "Resource name in the sent telemetry is wrong");
            Assert.AreEqual(target, remoteDependencyTelemetryActual.Target, true, "Resource target in the sent telemetry is wrong");
            Assert.AreEqual(type.ToString(), remoteDependencyTelemetryActual.Type, "DependencyKind in the sent telemetry is wrong");
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

            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), prefix: "rddf:");
            Assert.AreEqual(expectedVersion, remoteDependencyTelemetryActual.Context.GetInternalContext().SdkVersion);
        }

#endregion Helpers
    }
}
