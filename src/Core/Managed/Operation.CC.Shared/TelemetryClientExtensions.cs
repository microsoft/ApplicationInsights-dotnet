namespace Microsoft.ApplicationInsights
{
    using System;
    using System.ComponentModel;
    using Extensibility;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Extension class to telemetry client that creates operation object with the respective fields initialized.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TelemetryClientExtensions
    {
        /// <summary>
        /// Start operation creates an operation object with a respective telemetry item. 
        /// </summary>
        /// <typeparam name="T">Type of the telemetry item.</typeparam>
        /// <param name="telemetryClient">Telemetry client object.</param>
        /// <param name="operationName">Name of the operation that customer is planning to propagate.</param>
        /// <returns>Operation item object with a new telemetry item having current start time and timestamp.</returns>
        public static IOperationHolder<T> StartOperation<T>(this TelemetryClient telemetryClient, string operationName) where T : OperationTelemetry, new()
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException("Telemetry client cannot be null.");
            }

            var operation = new CallContextBasedOperationHolder<T>(telemetryClient, new T());
            operation.Telemetry.Start();

            // Parent context store is assigned to operation that is used to restore call context.
            operation.ParentContext = CallContextHelpers.GetCurrentOperationContextFromCallContext();

            if (string.IsNullOrEmpty(operation.Telemetry.Context.Operation.RootName) && !string.IsNullOrEmpty(operationName))
            {
                operation.Telemetry.Context.Operation.RootName = operationName;
            }

            telemetryClient.Initialize(operation.Telemetry);

            if (string.IsNullOrEmpty(operation.Telemetry.Context.Operation.Id))
            {
                operation.Telemetry.GenerateOperationId();
            }

            // Update the call context to store certain fields that can be used for subsequent operations.
            var operationContext = new OperationContextForCallContext();
            operationContext.ParentOperationId = operation.Telemetry.Context.Operation.Id;
            operationContext.RootOperationId = operation.Telemetry.Context.Operation.RootId;
            operationContext.OperationName = operation.Telemetry.Context.Operation.RootName;
            CallContextHelpers.SaveOperationContextToCallContext(operationContext);

            return operation;
        }

        /// <summary>
        /// Stop operation computes the duration of the operation and tracks it using the respective telemetry client.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client object.</param>
        /// <param name="operation">Operation object to compute duration and track.</param>
        public static void StopOperation<T>(this TelemetryClient telemetryClient, IOperationHolder<T> operation)
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException("telemetryClient");
            }

            if (operation == null)
            {
                CoreEventSource.Log.OperationIsNullWarning();
                return;
            }

            operation.Dispose();
        }
    }
}
