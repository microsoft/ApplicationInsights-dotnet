namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;

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
            this.stubTelemetryChannel = new StubTelemetryChannel {OnSend = item => this.sendItems.Add(item)};

            this.configuration = new TelemetryConfiguration
            {
                InstrumentationKey = Guid.NewGuid().ToString(),
                TelemetryChannel = stubTelemetryChannel
            };

            this.fakeSqlClientDiagnosticSource = new FakeSqlClientDiagnosticSource();
            this.sqlClientDiagnosticSourceListener = new SqlClientDiagnosticSourceListener(configuration);
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.sqlClientDiagnosticSourceListener.Dispose();
            this.fakeSqlClientDiagnosticSource.Dispose();
            this.configuration.Dispose();
            this.stubTelemetryChannel.Dispose();
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
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            var afterExecuteEventData = new
            {
                OperationId = operationId,
                Operation = "ExecuteReader",
                ConnectionId = sqlConnection.ClientConnectionId,
                Command = sqlCommand,
                Statistics = sqlConnection.RetrieveStatistics(),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterExecuteCommand,
                afterExecuteEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(afterExecuteEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(sqlCommand.CommandText, dependencyTelemetry.Data);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.AreEqual(1000000L, dependencyTelemetry.Duration.Ticks);
            Assert.IsTrue(dependencyTelemetry.Timestamp.Ticks < now.Ticks);
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
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeExecuteCommand,
                beforeExecuteEventData);

            var commandErrorEventData = new
            {
                OperationId = operationId,
                Operation = "ExecuteReader",
                ConnectionId = sqlConnection.ClientConnectionId,
                Command = sqlCommand,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorExecuteCommand,
                commandErrorEventData);

            var exceptionTelemetry = (ExceptionTelemetry)this.sendItems.Single();

            Assert.AreSame(commandErrorEventData.Exception, exceptionTelemetry.Exception);
            Assert.AreEqual(commandErrorEventData.Exception.Message, exceptionTelemetry.Message);
            Assert.IsTrue(exceptionTelemetry.Timestamp.Ticks < now.Ticks);
        }

        [TestMethod]
        public void TracksConnectionOpened()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeOpenEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeOpenConnection,
                beforeOpenEventData);

            var afterOpenEventData = new
            {
                OperationId = operationId,
                Operation = "Open",
                ConnectionId = sqlConnection.ClientConnectionId,
                Connection = sqlConnection,
                Statistics = sqlConnection.RetrieveStatistics(),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;
            
            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterOpenConnection,
                afterOpenEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(afterOpenEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(afterOpenEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master | " + afterOpenEventData.Operation, dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.AreEqual(1000000L, dependencyTelemetry.Duration.Ticks);
            Assert.IsTrue(dependencyTelemetry.Timestamp.Ticks < now.Ticks);
        }
        
        [TestMethod]
        public void TracksConnectionOpenedError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);
            
            var beforeOpenEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeOpenConnection,
                beforeOpenEventData);

            var errorOpenEventData = new
            {
                OperationId = operationId,
                Operation = "Open",
                ConnectionId = sqlConnection.ClientConnectionId,
                Connection = sqlConnection,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorOpenConnection,
                errorOpenEventData);

            var exceptionTelemetry = (ExceptionTelemetry)this.sendItems.Single();

            Assert.AreSame(errorOpenEventData.Exception, exceptionTelemetry.Exception);
            Assert.AreEqual(errorOpenEventData.Exception.Message, exceptionTelemetry.Message);
            Assert.IsTrue(exceptionTelemetry.Timestamp.Ticks < now.Ticks);
        }

        [TestMethod]
        public void TracksConnectionClosed()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeCloseEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCloseConnection,
                beforeCloseEventData);

            var afterCloseEventData = new
            {
                OperationId = operationId,
                Operation = "Close",
                ConnectionId = sqlConnection.ClientConnectionId,
                Connection = sqlConnection,
                Statistics = sqlConnection.RetrieveStatistics(),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterCloseConnection,
                afterCloseEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(afterCloseEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(afterCloseEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master | " + afterCloseEventData.Operation, dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.AreEqual(1000000L, dependencyTelemetry.Duration.Ticks);
            Assert.IsTrue(dependencyTelemetry.Timestamp.Ticks < now.Ticks);
        }

        [TestMethod]
        public void TracksConnectionCloseError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeOpenEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCloseConnection,
                beforeOpenEventData);

            var errorOpenEventData = new
            {
                OperationId = operationId,
                Operation = "Close",
                ConnectionId = sqlConnection.ClientConnectionId,
                Connection = sqlConnection,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorCloseConnection,
                errorOpenEventData);

            var exceptionTelemetry = (ExceptionTelemetry)this.sendItems.Single();

            Assert.AreSame(errorOpenEventData.Exception, exceptionTelemetry.Exception);
            Assert.AreEqual(errorOpenEventData.Exception.Message, exceptionTelemetry.Message);
            Assert.IsTrue(exceptionTelemetry.Timestamp.Ticks < now.Ticks);
        }

        [TestMethod]
        public void TracksTransactionCommitted()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeCommitEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCommitTransaction,
                beforeCommitEventData);

            var afterCommitEventData = new
            {
                OperationId = operationId,
                Operation = "Commit",
                IsolationLevel = IsolationLevel.Snapshot,
                Connection = sqlConnection,
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterCommitTransaction,
                afterCommitEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(afterCommitEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(afterCommitEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual(
                "(localdb)\\MSSQLLocalDB | master | " + afterCommitEventData.Operation + " | " + afterCommitEventData.IsolationLevel, 
                dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.AreEqual(1000000L, dependencyTelemetry.Duration.Ticks);
            Assert.IsTrue(dependencyTelemetry.Timestamp.Ticks < now.Ticks);
        }

        [TestMethod]
        public void TracksTransactionCommitError()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeCommitEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeCommitTransaction,
                beforeCommitEventData);

            var errorCommitEventData = new
            {
                OperationId = operationId,
                Operation = "Commit",
                IsolationLevel = IsolationLevel.Snapshot,
                Connection = sqlConnection,
                Exception = new Exception("Boom!"),
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlErrorCommitTransaction,
                errorCommitEventData);

            var exceptionTelemetry = (ExceptionTelemetry)this.sendItems.Single();

            Assert.AreSame(errorCommitEventData.Exception, exceptionTelemetry.Exception);
            Assert.AreEqual(errorCommitEventData.Exception.Message, exceptionTelemetry.Message);
            Assert.IsTrue(exceptionTelemetry.Timestamp.Ticks < now.Ticks);
        }

        [TestMethod]
        public void TracksTransactionRolledBack()
        {
            var operationId = Guid.NewGuid();
            var sqlConnection = new SqlConnection(TestConnectionString);

            var beforeRollbackEventData = new
            {
                OperationId = operationId,
                Timestamp = 1000000L
            };

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlBeforeRollbackTransaction,
                beforeRollbackEventData);

            var afterRollbackEventData = new
            {
                OperationId = operationId,
                Operation = "Rollback",
                IsolationLevel = IsolationLevel.Snapshot,
                Connection = sqlConnection,
                Timestamp = 2000000L
            };

            var now = DateTimeOffset.UtcNow;

            this.fakeSqlClientDiagnosticSource.Write(
                SqlClientDiagnosticSourceListener.SqlAfterRollbackTransaction,
                afterRollbackEventData);

            var dependencyTelemetry = (DependencyTelemetry)this.sendItems.Single();

            Assert.AreEqual(afterRollbackEventData.OperationId.ToString("N"), dependencyTelemetry.Id);
            Assert.AreEqual(afterRollbackEventData.Operation, dependencyTelemetry.Data);
            Assert.AreEqual(
                "(localdb)\\MSSQLLocalDB | master | " + afterRollbackEventData.Operation + " | " + afterRollbackEventData.IsolationLevel,
                dependencyTelemetry.Name);
            Assert.AreEqual("(localdb)\\MSSQLLocalDB | master", dependencyTelemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.SQL, dependencyTelemetry.Type);
            Assert.IsTrue((bool)dependencyTelemetry.Success);
            Assert.AreEqual(1000000L, dependencyTelemetry.Duration.Ticks);
            Assert.IsTrue(dependencyTelemetry.Timestamp.Ticks < now.Ticks);
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