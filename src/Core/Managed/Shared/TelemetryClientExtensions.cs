namespace Microsoft.ApplicationInsights
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
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
            return StartOperation<T>(telemetryClient, operationName, operationId: null, parentOperationId: null);
        }

        /// <summary>
        /// Start operation creates an operation object with a respective telemetry item. 
        /// </summary>
        /// <typeparam name="T">Type of the telemetry item.</typeparam>
        /// <param name="telemetryClient">Telemetry client object.</param>
        /// <param name="operationName">Name of the operation that customer is planning to propagate.</param>
        /// <param name="operationId">Operation ID to set in the new operation.</param>
        /// <param name="parentOperationId">Optional parent operation ID to set in the new operation.</param>
        /// <returns>Operation item object with a new telemetry item having current start time and timestamp.</returns>
        public static IOperationHolder<T> StartOperation<T>(this TelemetryClient telemetryClient, string operationName, string operationId, string parentOperationId = null) where T : OperationTelemetry, new()
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException("Telemetry client cannot be null.");
            }

            var operationTelemetry = new T();

            if (string.IsNullOrEmpty(operationTelemetry.Name) && !string.IsNullOrEmpty(operationName))
            {
                operationTelemetry.Name = operationName;
            }

            if (string.IsNullOrEmpty(operationTelemetry.Context.Operation.Id) && !string.IsNullOrEmpty(operationId))
            {
                operationTelemetry.Context.Operation.Id = operationId;
            }

            if (string.IsNullOrEmpty(operationTelemetry.Context.Operation.ParentId) && !string.IsNullOrEmpty(parentOperationId))
            {
                operationTelemetry.Context.Operation.ParentId = parentOperationId;
            }

            return StartOperation(telemetryClient, operationTelemetry);
        }

        /// <summary>
        /// Creates an operation object with a given telemetry item. 
        /// </summary>
        /// <typeparam name="T">Type of the telemetry item.</typeparam>
        /// <param name="telemetryClient">Telemetry client object.</param>
        /// <param name="operationTelemetry">Operation to start.</param>
        /// <returns>Operation item object with a new telemetry item having current start time and timestamp.</returns>
        public static IOperationHolder<T> StartOperation<T>(this TelemetryClient telemetryClient, T operationTelemetry) where T : OperationTelemetry
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException("Telemetry client cannot be null.");
            }

            if (operationTelemetry == null)
            {
                throw new ArgumentNullException("operationTelemetry cannot be null.");
            }

            var operationHolder = new OperationHolder<T>(telemetryClient, operationTelemetry)
            {
                // Parent context store is assigned to operation that is used to restore call context.
                ParentContext = CallContextHelpers.GetCurrentOperationContext()
            };

            telemetryClient.Initialize(operationTelemetry);

            // Initialize operation id if it wasn't initialized by telemetry initializers
            if (string.IsNullOrEmpty(operationTelemetry.Id))
            {
                operationTelemetry.GenerateOperationId();
            }

            // If the operation is not executing in the context of any other operation
            // set its name and id as a context (root) operation name and id
            if (string.IsNullOrEmpty(operationTelemetry.Context.Operation.Id))
            {
                operationTelemetry.Context.Operation.Id = operationTelemetry.Id;
            }

            if (string.IsNullOrEmpty(operationTelemetry.Context.Operation.Name))
            {
                operationTelemetry.Context.Operation.Name = operationTelemetry.Name;
            }

            bool isActivityEnabled = false;
#if !NET40
            isActivityEnabled = ActivityProxy.TryRun(() =>
            {
                bool operationNameIsSet = false;
                var activity = new Activity("Internal");
                if (!string.IsNullOrEmpty(operationTelemetry.Context.Operation.Name))
                {
                    activity.AddTag("OperationName", operationTelemetry.Context.Operation.Name);
                    operationNameIsSet = true;
                }

                if (Activity.Current == null)
                {
                    if (!string.IsNullOrEmpty(operationTelemetry.Context.Operation.Id))
                    {
                        activity.SetParentId(operationTelemetry.Context.Operation.Id);
                    }
                                else if (!string.IsNullOrEmpty(operationTelemetry.Context.Operation.ParentId))
                    {
                        activity.SetParentId(operationTelemetry.Context.Operation.ParentId);
                    }

                    operationTelemetry.Id = activity.ParentId;
                }
                else
                {
                    if (!operationNameIsSet)
                    {
                        var parentOperationName = Activity.Current.Tags.FirstOrDefault(t => t.Key == "OperationName").Value;
                        if (!string.IsNullOrEmpty(parentOperationName))
                        {
                            activity.AddTag("OperationName", parentOperationName);
                        }
                    }

                    operationTelemetry.Id = Activity.Current.Id;
                }

                activity.Start();
                return true;
            });
#endif
            operationTelemetry.Start();

            if (!isActivityEnabled)
            {
                // Update the call context to store certain fields that can be used for subsequent operations.
                var operationContext = new OperationContextForCallContext();
                operationContext.ParentOperationId = operationTelemetry.Id;
                operationContext.RootOperationId = operationTelemetry.Context.Operation.Id;
                operationContext.RootOperationName = operationTelemetry.Context.Operation.Name;
                CallContextHelpers.SaveOperationContext(operationContext);
            }

            return operationHolder;
        }

        /// <summary>
        /// Stop operation computes the duration of the operation and tracks it using the respective telemetry client.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client object.</param>
        /// <param name="operation">Operation object to compute duration and track.</param>
        public static void StopOperation<T>(this TelemetryClient telemetryClient, IOperationHolder<T> operation) where T : OperationTelemetry
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
