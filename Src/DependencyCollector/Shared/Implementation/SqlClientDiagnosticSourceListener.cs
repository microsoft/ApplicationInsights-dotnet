namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Web.Implementation;

    internal class SqlClientDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        // Event ids defined at: https://github.com/dotnet/corefx/blob/master/src/System.Data.SqlClient/src/System/Data/SqlClient/SqlClientDiagnosticListenerExtensions.cs
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        public const string SqlBeforeExecuteCommand = SqlClientPrefix + "WriteCommandBefore";
        public const string SqlAfterExecuteCommand = SqlClientPrefix + "WriteCommandAfter";
        public const string SqlErrorExecuteCommand = SqlClientPrefix + "WriteCommandError";
        
        public const string SqlBeforeOpenConnection = SqlClientPrefix + "WriteConnectionOpenBefore";
        public const string SqlAfterOpenConnection = SqlClientPrefix + "WriteConnectionOpenAfter";
        public const string SqlErrorOpenConnection = SqlClientPrefix + "WriteConnectionOpenError";

        public const string SqlBeforeCloseConnection = SqlClientPrefix + "WriteConnectionCloseBefore";
        public const string SqlAfterCloseConnection = SqlClientPrefix + "WriteConnectionCloseAfter";
        public const string SqlErrorCloseConnection = SqlClientPrefix + "WriteConnectionCloseError";

        public const string SqlBeforeCommitTransaction = SqlClientPrefix + "WriteTransactionCommitBefore";
        public const string SqlAfterCommitTransaction = SqlClientPrefix + "WriteTransactionCommitAfter";
        public const string SqlErrorCommitTransaction = SqlClientPrefix + "WriteTransactionCommitError";

        public const string SqlBeforeRollbackTransaction = SqlClientPrefix + "WriteTransactionRollbackBefore";
        public const string SqlAfterRollbackTransaction = SqlClientPrefix + "WriteTransactionRollbackAfter";
        public const string SqlErrorRollbackTransaction = SqlClientPrefix + "WriteTransactionRollbackError";

        private const string SqlClientPrefix = "System.Data.SqlClient.";

        private readonly TelemetryClient client;
        private readonly SqlClientDiagnosticSourceSubscriber subscriber;

        private readonly ConcurrentDictionary<Guid, long> operationStartTimestamps = new ConcurrentDictionary<Guid, long>();

        public SqlClientDiagnosticSourceListener(TelemetryConfiguration configuration)
        {
            this.client = new TelemetryClient(configuration);
            this.client.Context.GetInternalContext().SdkVersion =
                SdkVersionUtils.GetSdkVersion("rdd" + RddSource.DiagnosticSourceCore + ":");

            this.subscriber = new SqlClientDiagnosticSourceSubscriber(this);
        }

        public void Dispose()
        {
            if (this.subscriber != null)
            {
                this.subscriber.Dispose();
            }
        }
        
        void IObserver<KeyValuePair<string, object>>.OnCompleted()
        {
        }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
        {
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> evnt)
        {
            switch (evnt.Key)
            {
                case SqlBeforeExecuteCommand:
                {
                    var operationId = (Guid)CommandBefore.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);

                    this.operationStartTimestamps.TryAdd(
                        operationId,
                        CommandBefore.Timestamp.Fetch(evnt.Value) as long? ?? Stopwatch.GetTimestamp()); // TODO corefx#20748 - timestamp missing from event data

                    break;
                }

                case SqlAfterExecuteCommand:
                {
                    var operationId = (Guid)CommandAfter.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.OnAfterCommandEvent(
                        operationId,
                        (string)CommandAfter.Operation.Fetch(evnt.Value),
                        (Guid)CommandAfter.ConnectionId.Fetch(evnt.Value),
                        (SqlCommand)CommandAfter.Command.Fetch(evnt.Value),
                        (IDictionary)CommandAfter.Statistics.Fetch(evnt.Value),
                        (long)CommandAfter.Timestamp.Fetch(evnt.Value));

                    break;
                }

                case SqlErrorExecuteCommand:
                {
                    var operationId = (Guid)CommandError.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.OnCommandErrorEvent(
                        operationId,
                        (string)CommandError.Operation.Fetch(evnt.Value),
                        (Guid)CommandError.ConnectionId.Fetch(evnt.Value),
                        (SqlCommand)CommandError.Command.Fetch(evnt.Value),
                        (Exception)CommandError.Exception.Fetch(evnt.Value),
                        (long)CommandError.Timestamp.Fetch(evnt.Value));

                    break;
                }
                    
                case SqlBeforeOpenConnection:
                case SqlBeforeCloseConnection:
                {
                    var operationId = (Guid)ConnectionBefore.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.operationStartTimestamps.TryAdd(
                        operationId,
                        (long)ConnectionBefore.Timestamp.Fetch(evnt.Value));

                    break;
                }

                case SqlErrorOpenConnection:
                {
                    var operationId = (Guid)ConnectionError.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.OnErrorConnectionEvent(
                        operationId,
                        (string)ConnectionError.Operation.Fetch(evnt.Value),
                        (Guid)ConnectionError.ConnectionId.Fetch(evnt.Value),
                        (SqlConnection)ConnectionError.Connection.Fetch(evnt.Value),
                        (Exception)ConnectionError.Exception.Fetch(evnt.Value),
                        (long)ConnectionError.Timestamp.Fetch(evnt.Value));

                    break;
                }

                case SqlBeforeCommitTransaction:
                case SqlBeforeRollbackTransaction:
                {
                    var operationId = (Guid)TransactionBefore.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.operationStartTimestamps.TryAdd(
                        operationId,
                        (long)TransactionBefore.Timestamp.Fetch(evnt.Value));

                    break;
                }

                case SqlAfterCommitTransaction:
                case SqlAfterRollbackTransaction:
                {
                    var operationId = (Guid)TransactionAfter.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.OnAfterTransactionEvent(
                        operationId,
                        (string)TransactionAfter.Operation.Fetch(evnt.Value),
                        (IsolationLevel)TransactionAfter.IsolationLevel.Fetch(evnt.Value),
                        (SqlConnection)TransactionAfter.Connection.Fetch(evnt.Value),
                        (long)TransactionAfter.Timestamp.Fetch(evnt.Value));

                    break;
                }

                case SqlErrorCommitTransaction:
                case SqlErrorRollbackTransaction:
                {
                    var operationId = (Guid)TransactionError.OperationId.Fetch(evnt.Value);

                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberCallbackCalled(operationId, evnt.Key);
                    
                    this.OnErrorTransactionEvent(
                        operationId,
                        (string)TransactionError.Operation.Fetch(evnt.Value),
                        (IsolationLevel)TransactionError.IsolationLevel.Fetch(evnt.Value),
                        (SqlConnection)TransactionError.Connection.Fetch(evnt.Value),
                        (Exception)TransactionError.Exception.Fetch(evnt.Value),
                        (long)TransactionError.Timestamp.Fetch(evnt.Value));

                    break;
                }
            }
        }

        private static void InitializeTelemetry(ITelemetry telemetry, Guid operationId)
        {
            var activity = Activity.Current;

            if (activity != null)
            {
                telemetry.Context.Operation.Id = activity.RootId;
                telemetry.Context.Operation.ParentId = activity.ParentId;

                foreach (var item in activity.Baggage)
                {
                    if (!telemetry.Context.Properties.ContainsKey(item.Key))
                    {
                        telemetry.Context.Properties[item.Key] = item.Value;
                    }
                }
            }
            else
            {
                telemetry.Context.Operation.Id = operationId.ToString("N");
            }
        }

        private static void ConfigureExceptionTelemetry(DependencyTelemetry telemetry, Exception exception)
        {
            telemetry.Success = false;
            telemetry.Properties["Exception"] = exception.ToInvariantString();

            if (exception is SqlException sqlException)
            {
                telemetry.ResultCode = sqlException.Number.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void OnAfterCommandEvent(
            Guid operationId,
            string operation,
            Guid connectionId,
            SqlCommand command,
            IDictionary statistics,
            long endTimestamp)
        {
            var telemetry = this.CreateCommandTelemetry(operationId, command, endTimestamp);
            
            this.client.Track(telemetry);
        }

        private void OnCommandErrorEvent(
            Guid operationId,
            string operation,
            Guid connectionId,
            SqlCommand command,
            Exception exception,
            long endTimestamp)
        {
            var telemetry = this.CreateCommandTelemetry(operationId, command, endTimestamp);

            ConfigureExceptionTelemetry(telemetry, exception);

            this.client.Track(telemetry);
        }

        private DependencyTelemetry CreateCommandTelemetry(Guid operationId, SqlCommand command, long endTimestamp)
        {
            // Call DeriveDuration first to ensure we remove any start timestamp from operationStartTimestamps
            var duration = this.DeriveDuration(operationId, endTimestamp);

            var dependencyName = string.Empty;
            var target = string.Empty;

            if (command.Connection != null)
            {
                target = string.Join(" | ", command.Connection.DataSource, command.Connection.Database);

                var commandName = command.CommandType == CommandType.StoredProcedure
                    ? command.CommandText
                    : string.Empty;

                dependencyName = string.IsNullOrEmpty(commandName)
                    ? string.Join(" | ", command.Connection.DataSource, command.Connection.Database)
                    : string.Join(" | ", command.Connection.DataSource, command.Connection.Database, commandName);
            }

            var telemetry = new DependencyTelemetry()
            {
                Id = operationId.ToString("N"),
                Name = dependencyName,
                Type = RemoteDependencyConstants.SQL,
                Target = target,
                Data = command.CommandText,
                Duration = duration,
                Timestamp = DateTimeOffset.UtcNow - duration,
                Success = true
            };

            InitializeTelemetry(telemetry, operationId);

            return telemetry;
        }

        private void OnErrorConnectionEvent(
            Guid operationId,
            string operation,
            Guid connectionId,
            SqlConnection connection,
            Exception exception,
            long endTimestamp)
        {
            var duration = this.DeriveDuration(operationId, endTimestamp);

            var telemetry = new DependencyTelemetry()
            {
                Id = operationId.ToString("N"),
                Name = string.Join(" | ", connection.DataSource, connection.Database, operation),
                Type = RemoteDependencyConstants.SQL,
                Target = string.Join(" | ", connection.DataSource, connection.Database),
                Data = operation,
                Duration = duration,
                Timestamp = DateTimeOffset.UtcNow - duration,
                Success = true
            };

            InitializeTelemetry(telemetry, operationId);
            ConfigureExceptionTelemetry(telemetry, exception);

            this.client.Track(telemetry);
        }

        private void OnAfterTransactionEvent(
            Guid operationId,
            string operation,
            IsolationLevel isolationLevel,
            SqlConnection connection,
            long endTimestamp)
        {
            var telemetry = this.CreateTransactionTelemetry(operationId, operation, isolationLevel, connection, endTimestamp);

            this.client.Track(telemetry);
        }

        private void OnErrorTransactionEvent(
            Guid operationId,
            string operation,
            IsolationLevel isolationLevel,
            SqlConnection connection,
            Exception exception,
            long endTimestamp)
        {
            var telemetry = this.CreateTransactionTelemetry(operationId, operation, isolationLevel, connection, endTimestamp);

            ConfigureExceptionTelemetry(telemetry, exception);

            this.client.Track(telemetry);
        }

        private DependencyTelemetry CreateTransactionTelemetry(
            Guid operationId, string operation, IsolationLevel isolationLevel, SqlConnection connection, long endTimestamp)
        {
            var duration = this.DeriveDuration(operationId, endTimestamp);

            var telemetry = new DependencyTelemetry()
            {
                Id = operationId.ToString("N"),
                Name = string.Join(" | ", connection.DataSource, connection.Database, operation, isolationLevel),
                Type = RemoteDependencyConstants.SQL,
                Target = string.Join(" | ", connection.DataSource, connection.Database),
                Data = operation,
                Duration = duration,
                Timestamp = DateTimeOffset.UtcNow - duration,
                Success = true
            };

            InitializeTelemetry(telemetry, operationId);

            return telemetry;
        }

        private TimeSpan DeriveDuration(Guid operationId, long endTimestamp)
        {
            return this.operationStartTimestamps.TryRemove(operationId, out var startTimestamp)
                ? TimeSpan.FromTicks(endTimestamp - startTimestamp)
                : default(TimeSpan);
        }

        #region Fetchers

        // Fetchers for execute command before event
        private static class CommandBefore
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for execute command after event
        private static class CommandAfter
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Operation = new PropertyFetcher(nameof(Operation));
            public static readonly PropertyFetcher ConnectionId = new PropertyFetcher(nameof(ConnectionId));
            public static readonly PropertyFetcher Command = new PropertyFetcher(nameof(Command));
            public static readonly PropertyFetcher Statistics = new PropertyFetcher(nameof(Statistics));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for execute command error event
        private static class CommandError
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Operation = new PropertyFetcher(nameof(Operation));
            public static readonly PropertyFetcher ConnectionId = new PropertyFetcher(nameof(ConnectionId));
            public static readonly PropertyFetcher Command = new PropertyFetcher(nameof(Command));
            public static readonly PropertyFetcher Exception = new PropertyFetcher(nameof(Exception));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for connection open/close before events
        private static class ConnectionBefore
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for connection open/close after events
        private static class ConnectionAfter
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Operation = new PropertyFetcher(nameof(Operation));
            public static readonly PropertyFetcher ConnectionId = new PropertyFetcher(nameof(ConnectionId));
            public static readonly PropertyFetcher Connection = new PropertyFetcher(nameof(Connection));
            public static readonly PropertyFetcher Statistics = new PropertyFetcher(nameof(Statistics));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for connection open/close error events
        private static class ConnectionError
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Operation = new PropertyFetcher(nameof(Operation));
            public static readonly PropertyFetcher ConnectionId = new PropertyFetcher(nameof(ConnectionId));
            public static readonly PropertyFetcher Connection = new PropertyFetcher(nameof(Connection));
            public static readonly PropertyFetcher Exception = new PropertyFetcher(nameof(Exception));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for transaction commit/rollback before events
        private static class TransactionBefore
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for transaction commit/rollback after events
        private static class TransactionAfter
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Operation = new PropertyFetcher(nameof(Operation));
            public static readonly PropertyFetcher IsolationLevel = new PropertyFetcher(nameof(IsolationLevel));
            public static readonly PropertyFetcher Connection = new PropertyFetcher(nameof(Connection));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        // Fetchers for transaction commit/rollback error events
        private static class TransactionError
        {
            public static readonly PropertyFetcher OperationId = new PropertyFetcher(nameof(OperationId));
            public static readonly PropertyFetcher Operation = new PropertyFetcher(nameof(Operation));
            public static readonly PropertyFetcher IsolationLevel = new PropertyFetcher(nameof(IsolationLevel));
            public static readonly PropertyFetcher Connection = new PropertyFetcher(nameof(Connection));
            public static readonly PropertyFetcher Exception = new PropertyFetcher(nameof(Exception));
            public static readonly PropertyFetcher Timestamp = new PropertyFetcher(nameof(Timestamp));
        }

        #endregion

        private sealed class SqlClientDiagnosticSourceSubscriber : IObserver<DiagnosticListener>, IDisposable
        {
            private readonly SqlClientDiagnosticSourceListener sqlDiagnosticListener;
            private readonly IDisposable listenerSubscription;

            private IDisposable eventSubscription;

            internal SqlClientDiagnosticSourceSubscriber(SqlClientDiagnosticSourceListener listener)
            {
                this.sqlDiagnosticListener = listener;

                try
                {
                    this.listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
                }
                catch (Exception ex)
                {
                    DependencyCollectorEventSource.Log.SqlClientDiagnosticSubscriberFailedToSubscribe(ex.ToInvariantString());
                }
            }

            public void OnNext(DiagnosticListener value)
            {
                if (value != null)
                {
                    if (value.Name == DiagnosticListenerName)
                    {
                        this.eventSubscription = value.Subscribe(this.sqlDiagnosticListener);
                    }
                }
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void Dispose()
            {
                if (this.eventSubscription != null)
                {
                    this.eventSubscription.Dispose();
                }

                if (this.listenerSubscription != null)
                {
                    this.listenerSubscription.Dispose();
                }
            }
        }
    }
}