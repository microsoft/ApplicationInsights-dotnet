namespace Microsoft.ApplicationInsights
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Extension class providing operation lifecycle helpers for <see cref="TelemetryClient"/>.
    /// Enables automatic correlation of telemetry with <see cref="Activity"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TelemetryClientExtensions
    {
        /// <summary>
        /// Starts a new telemetry operation (e.g., request, dependency, etc.) with the specified name.
        /// Creates and starts an <see cref="Activity"/> that defines the operation context.
        /// </summary>
        /// <typeparam name="T">The type of telemetry item (e.g., <see cref="RequestTelemetry"/> or <see cref="DependencyTelemetry"/>).</typeparam>
        /// <param name="telemetryClient">The <see cref="TelemetryClient"/> instance used to create and track the operation.</param>
        /// <param name="operationName">The operation name, used as the <see cref="Activity.DisplayName"/>.</param>
        /// <returns>
        /// An <see cref="IOperationHolder{T}"/> that holds the telemetry and the corresponding activity.
        /// Disposing this object stops the operation and sends telemetry automatically.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetryClient"/> is null.</exception>
        public static IOperationHolder<T> StartOperation<T>(
            this TelemetryClient telemetryClient, string operationName)
            where T : OperationTelemetry, new()
        {
            return StartOperation<T>(telemetryClient, operationName, operationId: null, parentOperationId: null);
        }

        /// <summary>
        /// Starts a new telemetry operation with a specific operation and parent operation ID for correlation.
        /// </summary>
        /// <typeparam name="T">The type of telemetry item (e.g., <see cref="RequestTelemetry"/> or <see cref="DependencyTelemetry"/>).</typeparam>
        /// <param name="telemetryClient">The <see cref="TelemetryClient"/> instance used to create and track the operation.</param>
        /// <param name="operationName">The name of the operation to create.</param>
        /// <param name="operationId">The W3C trace ID (32-character hex string) to use for the operation.</param>
        /// <param name="parentOperationId">The optional parent span ID (16-character hex string) to correlate with the parent operation.</param>
        /// <returns>
        /// An <see cref="IOperationHolder{T}"/> containing the telemetry item and associated <see cref="Activity"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetryClient"/> is null.</exception>
        public static IOperationHolder<T> StartOperation<T>(
            this TelemetryClient telemetryClient,
            string operationName,
            string operationId,
            string parentOperationId = null)
            where T : OperationTelemetry, new()
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            var effectiveName = string.IsNullOrEmpty(operationName) ? typeof(T).Name : operationName;
            var kind = ResolveActivityKind<T>();
            var source = telemetryClient.TelemetryConfiguration.ApplicationInsightsActivitySource;
            ActivityContext parentContext = default;

            if (!string.IsNullOrEmpty(operationId) && operationId.Length == 32)
            {
                try
                {
                    var traceId = ActivityTraceId.CreateFromString(operationId.AsSpan());
                    var spanId = !string.IsNullOrEmpty(parentOperationId) && parentOperationId.Length == 16
                        ? ActivitySpanId.CreateFromString(parentOperationId.AsSpan())
                        : ActivitySpanId.CreateRandom();

                    parentContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
                }
                catch
                {
                    // ignore malformed IDs
                }
            }

            var activity = source.StartActivity(effectiveName, kind, parentContext);

            if (activity == null)
            {
                return new OperationHolder<T>(
                    telemetryClient,
                    new T { Name = effectiveName, Timestamp = DateTimeOffset.UtcNow },
                    null);
            }

            var telemetry = new T
            {
                Name = effectiveName,
                Timestamp = DateTimeOffset.UtcNow,
                Id = activity.SpanId.ToHexString(),
            };

            telemetry.Context.Operation.Id = activity.TraceId.ToHexString();
            telemetry.Context.Operation.ParentId = activity.ParentSpanId.ToHexString();

            return new OperationHolder<T>(telemetryClient, telemetry, activity);
        }

        /// <summary>
        /// Starts a telemetry operation using an existing telemetry object.
        /// This overload is useful when the telemetry item is pre-populated with metadata or custom properties.
        /// </summary>
        /// <typeparam name="T">The type of telemetry item.</typeparam>
        /// <param name="telemetryClient">The <see cref="TelemetryClient"/> used to create and track the operation.</param>
        /// <param name="operationTelemetry">The telemetry item to associate with the new operation.</param>
        /// <returns>
        /// An <see cref="IOperationHolder{T}"/> containing the provided telemetry and its associated activity.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetryClient"/> is null.</exception>
        public static IOperationHolder<T> StartOperation<T>(
            this TelemetryClient telemetryClient,
            T operationTelemetry)
            where T : OperationTelemetry
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            if (operationTelemetry == null)
            {
                throw new ArgumentNullException(nameof(operationTelemetry));
            }

            if (string.IsNullOrEmpty(operationTelemetry.Name))
            {
                operationTelemetry.Name = typeof(T).Name;
            }

            var kind = ResolveActivityKind<T>();
            var source = telemetryClient.TelemetryConfiguration.ApplicationInsightsActivitySource;
            ActivityContext parentContext = default;

            if (!string.IsNullOrEmpty(operationTelemetry.Context?.Operation?.Id) &&
                operationTelemetry.Context.Operation.Id.Length == 32)
            {
                try
                {
                    var traceId = ActivityTraceId.CreateFromString(operationTelemetry.Context.Operation.Id.AsSpan());
                    var spanId = !string.IsNullOrEmpty(operationTelemetry.Context.Operation.ParentId) &&
                                 operationTelemetry.Context.Operation.ParentId.Length == 16
                                 ? ActivitySpanId.CreateFromString(operationTelemetry.Context.Operation.ParentId.AsSpan())
                                 : ActivitySpanId.CreateRandom();

                    parentContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
                }
                catch
                {
                    // ignore malformed IDs
                }
            }

            var activity = source.StartActivity(operationTelemetry.Name, kind, parentContext);

            if (activity == null)
            {
                return new OperationHolder<T>(telemetryClient, operationTelemetry, null);
            }

            operationTelemetry.Timestamp = DateTimeOffset.UtcNow;
            operationTelemetry.Id = activity.SpanId.ToHexString();
            operationTelemetry.Context.Operation.Id = activity.TraceId.ToHexString();
            operationTelemetry.Context.Operation.ParentId = activity.ParentSpanId.ToHexString();

            return new OperationHolder<T>(telemetryClient, operationTelemetry, activity);
        }

        /// <summary>
        /// Starts a telemetry operation based on an existing <see cref="Activity"/> instance that carries trace context.
        /// The activity must have been created by the caller or extracted from incoming telemetry.
        /// </summary>
        /// <typeparam name="T">The type of telemetry item (request, dependency, etc.).</typeparam>
        /// <param name="telemetryClient">The <see cref="TelemetryClient"/> that will manage and emit the telemetry.</param>
        /// <param name="activity">The existing <see cref="Activity"/> to associate with this operation.</param>
        /// <returns>An <see cref="IOperationHolder{T}"/> linking telemetry and activity for unified tracking.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetryClient"/> or <paramref name="activity"/> is null.</exception>
        public static IOperationHolder<T> StartOperation<T>(this TelemetryClient telemetryClient,
                                                            Activity activity)
                                                            where T : OperationTelemetry, new()
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var effectiveName = activity.DisplayName ?? typeof(T).Name;
            var kind = ResolveActivityKind<T>();
            var source = telemetryClient.TelemetryConfiguration.ApplicationInsightsActivitySource;

            // ensure we always create a *listened* Activity even if input wasn’t from an ActivitySource
            ActivityContext parentContext = new (
                activity.TraceId,
                activity.SpanId != default ? activity.SpanId : ActivitySpanId.CreateRandom(),
                activity.ActivityTraceFlags,
                activity.TraceStateString);

            var newActivity = source.StartActivity(effectiveName, kind, parentContext);

            if (newActivity == null)
            {
                // no listeners or sampled out → inert holder
                return new OperationHolder<T>(
                    telemetryClient,
                    new T { Name = effectiveName, Timestamp = DateTimeOffset.UtcNow },
                    null);
            }

            // Copy baggage and tags from the provided Activity
            foreach (var kvp in activity.Baggage)
            {
                newActivity.SetBaggage(kvp.Key, kvp.Value);
            }

            foreach (var kvp in activity.Tags)
            {
                newActivity.SetTag(kvp.Key, kvp.Value);
            }

            newActivity.TraceStateString = activity.TraceStateString;

            var telemetry = new T
            {
                Name = effectiveName,
                Timestamp = DateTimeOffset.UtcNow,
            };

            // No need to map IDs or context — Activity drives everything.
            return new OperationHolder<T>(telemetryClient, telemetry, newActivity);
        }

        /// <summary>
        /// Stops a telemetry operation started by <see cref="StartOperation{T}(TelemetryClient, string)"/> or its overloads.
        /// Disposes the operation holder, stopping the associated activity and emitting telemetry.
        /// </summary>
        /// <typeparam name="T">The type of telemetry item being tracked.</typeparam>
        /// <param name="telemetryClient">The <see cref="TelemetryClient"/> used to stop the operation.</param>
        /// <param name="operation">The <see cref="IOperationHolder{T}"/> representing the operation to stop.</param>
        public static void StopOperation<T>(
            this TelemetryClient telemetryClient,
            IOperationHolder<T> operation)
            where T : OperationTelemetry
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            if (operation == null)
            {
                CoreEventSource.Log.OperationIsNullWarning();
                return;
            }

            operation.Dispose(); // ensures Activity.Stop() is called
        }

        private static ActivityKind ResolveActivityKind<T>() where T : OperationTelemetry
        {
            if (typeof(T) == typeof(RequestTelemetry))
            {
                return ActivityKind.Server;
            }

            if (typeof(T) == typeof(DependencyTelemetry))
            {
                return ActivityKind.Client;
            }

            return ActivityKind.Internal;
        }
    }
}
