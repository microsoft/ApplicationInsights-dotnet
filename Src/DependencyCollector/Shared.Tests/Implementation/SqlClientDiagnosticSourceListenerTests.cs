namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.SqlClientDiagnostics;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [TestClass]
    public class SqlClientDiagnosticSourceListenerTests
    {
        private const string TestConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Database=master";
        
        private IList<ITelemetry> sendItems;
        private StubTelemetryChannel stubTelemetryChannel;
        private TelemetryConfiguration configuration;
        private FakeSqlClientDiagnosticSource fakeSqlClientDiagnosticSource;
        private SqlClientDiagnosticSourceListener sqlClientDiagnosticSourceListener;

        [TestInitialize]
        public void TestInit()
        {
            this.sendItems = new List<ITelemetry>();
            this.stubTelemetryChannel = new StubTelemetryChannel { OnSend = item => this.sendItems.Add(item) };

            this.configuration = new TelemetryConfiguration
            {
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryChannel = this.stubTelemetryChannel
            };

            this.fakeSqlClientDiagnosticSource = new FakeSqlClientDiagnosticSource();
            this.sqlClientDiagnosticSourceListener = new SqlClientDiagnosticSourceListener(this.configuration);
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.sqlClientDiagnosticSourceListener.Dispose();
            this.fakeSqlClientDiagnosticSource.Dispose();
            this.configuration.Dispose();
            this.stubTelemetryChannel.Dispose();

            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void InitializesTelemetryFromParentActivity()
        {
            var activity = new Activity("Current").AddBaggage("Stuff", "123");
            activity.Start();

            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                afterExecuteEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(activity.RootId, dependencyTelemetry.Context.Operation.Id);
            Assert.AreEqual(activity.Id, dependencyTelemetry.Context.Operation.ParentId);
            Assert.AreEqual("123", dependencyTelemetry.Properties["Stuff"]);
        }

        [TestMethod]
        public void TracksCommandExecuted()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";
            
            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);
            var start = DateTimeOffset.UtcNow;

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                afterExecuteEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(beforeExecuteEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(sqlCommand.CommandText, dependencyTelemetry.Data);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.IsTrue(dependencyTelemetry.Duration > TimeSpan.Zero);
            Assert.IsTrue(dependencyTelemetry.Duration < TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(Math.Abs((start - dependencyTelemetry.Timestamp).TotalMilliseconds) <= 16);
        }

        [TestMethod]
        public void TracksCommandExecutedWhenNoTimestamp()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)null
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = Stopwatch.GetTimestamp()
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                afterExecuteEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(beforeExecuteEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(sqlCommand.CommandText, dependencyTelemetry.Data);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.IsTrue(dependencyTelemetry.Duration > TimeSpan.Zero);
            Assert.IsTrue(dependencyTelemetry.Duration < TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void TracksCommandError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            var commandErrorEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorExecuteCommand,
                commandErrorEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(commandErrorEventData.Exception.ToInvariantString(), dependencyTelemetry.Properties["Exception"]);
            Assert.IsFalse(dependencyTelemetry.Success.Value);
        }

        [TestMethod]
        public void TracksCommandErrorWhenSqlException()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            // Need to create SqlException via reflection because ctor is not public!
            var sqlErrorCtor
                = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(c => c.GetParameters().Count() == 8);

            var sqlError = sqlErrorCtor.Invoke(
                new object[]
                {
                    42, // error number
                    default(byte),
                    default(byte),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0,
                    default(Exception)
                });

            var sqlErrorCollectionCtor
                = typeof(SqlErrorCollection).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single();

            var sqlErrorCollection = sqlErrorCollectionCtor.Invoke(new object[] { });

            typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sqlErrorCollection, new object[] { sqlError });
            
            var sqlExceptionCtor = typeof(SqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

            var sqlException = (SqlException)sqlExceptionCtor.Invoke(
                new object[]
                {
                    "Boom!",
                    sqlErrorCollection,
                    null,
                    Guid.NewGuid()
                });

            var commandErrorEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Exception = (Exception)sqlException,
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorExecuteCommand,
                commandErrorEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(commandErrorEventData.Exception.ToInvariantString(), dependencyTelemetry.Properties["Exception"]);
            Assert.IsFalse(dependencyTelemetry.Success.Value);
            Assert.AreEqual("42", dependencyTelemetry.ResultCode);
        }

        [TestMethod]
        public void TracksConnectionOpenedError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            
            var beforeOpenEventData = new
            {
                OperationId = operationId,
                Operation = "Open",
                Connection = sqlConnection,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeOpenConnection,
                beforeOpenEventData);

            var errorOpenEventData = new
            {
                OperationId = operationId,
                Connection = sqlConnection,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            var start = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorOpenConnection,
                errorOpenEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(beforeOpenEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(beforeOpenEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master | " + beforeOpenEventData.Operation, dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue(dependencyTelemetry.Duration > TimeSpan.Zero);
            Assert.IsTrue(dependencyTelemetry.Duration < TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(Math.Abs((start - dependencyTelemetry.Timestamp).TotalMilliseconds) <= 16);

            Assert.AreEqual(errorOpenEventData.Exception.ToInvariantString(), dependencyTelemetry.Properties["Exception"]);
            Assert.IsFalse(dependencyTelemetry.Success.Value);
        }

        [TestMethod]
        public void DoesNotTrackConnectionOpened()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeOpenEventData = new
            {
                OperationId = operationId,
                Operation = "Open",
                Connection = sqlConnection,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeOpenConnection,
                beforeOpenEventData);

            var afterOpenEventData = new
            {
                OperationId = operationId,
                Connection = sqlConnection
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterOpenConnection,
                afterOpenEventData);

            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void DoesNotTrackConnectionCloseError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeOpenEventData = new
            {
                OperationId = operationId,
                Operation = "Close",
                Connection = sqlConnection,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCloseConnection,
                beforeOpenEventData);

            var errorCloseEventData = new
            {
                OperationId = operationId,
                Connection = sqlConnection,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorCloseConnection,
                errorCloseEventData);

            Assert.AreEqual(0, this.sendItems.Count);
        }

        [TestMethod]
        public void TracksTransactionCommitted()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeCommitEventData = new
            {
                OperationId = operationId,
                Operation = "Commit",
                IsolationLevel = IsolationLevel.Snapshot,
                Connection = sqlConnection,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCommitTransaction,
                beforeCommitEventData);

            var afterCommitEventData = new
            {
                OperationId = operationId,
                Connection = sqlConnection,
                Timestamp = 2000000L
            };

            var start = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterCommitTransaction,
                afterCommitEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(beforeCommitEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(beforeCommitEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual(
                "(localdb)\\MSSQLLocalDB | master | " + beforeCommitEventData.Operation + " | " + beforeCommitEventData.IsolationLevel, 
                dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.IsTrue(dependencyTelemetry.Duration > TimeSpan.Zero);
            Assert.IsTrue(dependencyTelemetry.Duration < TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(Math.Abs((start - dependencyTelemetry.Timestamp).TotalMilliseconds) <= 16);
        }

        [TestMethod]
        public void TracksTransactionCommitError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeCommitEventData = new
            {
                OperationId = operationId,
                Operation = "Commit",
                IsolationLevel = IsolationLevel.Snapshot,
                Connection = sqlConnection,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCommitTransaction,
                beforeCommitEventData);

            var errorCommitEventData = new
            {
                OperationId = operationId,
                Connection = sqlConnection,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorCommitTransaction,
                errorCommitEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(errorCommitEventData.Exception.ToInvariantString(), dependencyTelemetry.Properties["Exception"]);
            Assert.IsFalse(dependencyTelemetry.Success.Value);
        }

        [TestMethod]
        public void TracksTransactionRolledBack()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeRollbackEventData = new
            {
                OperationId = operationId,
                Operation = "Rollback",
                IsolationLevel = IsolationLevel.Snapshot,
                TransactionName = "testTransactionName",
                Connection = sqlConnection,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeRollbackTransaction,
                beforeRollbackEventData);

            var afterRollbackEventData = new
            {
                OperationId = operationId,
                Connection = sqlConnection,
                Timestamp = 2000000L
            };

            var start = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterRollbackTransaction,
                afterRollbackEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(beforeRollbackEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(beforeRollbackEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual(
                "(localdb)\\MSSQLLocalDB | master | " + beforeRollbackEventData.Operation + " | " + beforeRollbackEventData.IsolationLevel,
                dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.IsTrue(dependencyTelemetry.Duration > TimeSpan.Zero);
            Assert.IsTrue(dependencyTelemetry.Duration < TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(Math.Abs((start - dependencyTelemetry.Timestamp).TotalMilliseconds) <= 16);
        }

        [TestMethod]
        public void MultiHost_OneListenerIsActive()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = 2000000L
            };

            using (var listener2 = new SqlClientDiagnosticSourceListener(this.configuration))
            {
                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                    beforeExecuteEventData);

                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                    afterExecuteEventData);

                Assert.AreEqual(1, this.sendItems.Count(t => t is DependencyTelemetry));
            }
        }

        [TestMethod]
        public void MultiHost_OneListenerThenAnotherIsActive()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = 2000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                afterExecuteEventData);

            Assert.AreEqual(1, this.sendItems.Count(t => t is DependencyTelemetry));

            this.sqlClientDiagnosticSourceListener.Dispose();

            using (var listener = new SqlClientDiagnosticSourceListener(this.configuration))
            {
                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                    beforeExecuteEventData);

                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                    afterExecuteEventData);

                Assert.AreEqual(2, this.sendItems.Count(t => t is DependencyTelemetry));
            }
        }

        [TestMethod]
        public void MultiHost_OneListnerIsActiveAfterDispose()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "select * from orders";

            var beforeExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = (long?)1000000L
            };

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Command = sqlCommand,
                Timestamp = 2000000L
            };

            using (var listener2 = new SqlClientDiagnosticSourceListener(this.configuration))
            {
                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                    beforeExecuteEventData);

                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                    afterExecuteEventData);

                Assert.AreEqual(1, this.sendItems.Count(t => t is DependencyTelemetry));

                this.sqlClientDiagnosticSourceListener.Dispose();

                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                    beforeExecuteEventData);

                this.fakeSqlClientDiagnosticSource.Write(
                    SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                    afterExecuteEventData);

                Assert.AreEqual(2, this.sendItems.Count(t => t is DependencyTelemetry));
            }
        }

        private class FakeSqlClientDiagnosticSource : IDisposable
        {
            private readonly DiagnosticListener listener;

            public FakeSqlClientDiagnosticSource()
            {
                this.listener = new DiagnosticListener(SqlClientDiagnosticSourceListener.DiagnosticListenerName);
            }

            public void Write(string name, object value)
            {
                this.listener.Write(name, value);
            }

            public void Dispose()
            {
                this.listener.Dispose();
            }
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}