namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Represents an ongoing telemetry operation that wraps a telemetry item and its associated Activity.
    /// In the OpenTelemetry-based shim, disposing copies the telemetry item onto the Activity (when the SDK created it)
    /// and stops the Activity so that the exporter emits it.
    /// </summary>
    internal sealed class OperationHolder<T> : IOperationHolder<T> where T : OperationTelemetry
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Activity activity;
        private readonly Activity suppressedActivity;
        private readonly bool applyTelemetryOnDispose;
        private readonly string initialOperationName;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationHolder{T}"/> class.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client associated with this operation.</param>
        /// <param name="telemetry">Telemetry item created for this operation.</param>
        /// <param name="activity">Activity that represents the operation context. May be null if sampled out or no listener.</param>
        public OperationHolder(TelemetryClient telemetryClient, T telemetry, Activity activity)
            : this(telemetryClient, telemetry, activity, null, applyTelemetryOnDispose: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationHolder{T}"/> class.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client associated with this operation.</param>
        /// <param name="telemetry">Telemetry item created for this operation.</param>
        /// <param name="activity">Activity that represents the operation context. May be null if sampled out or no listener.</param>
        /// <param name="suppressedActivity">An ambient activity that was suppressed to create a root operation and should be restored on dispose.</param>
        /// <param name="applyTelemetryOnDispose">
        /// True when the SDK created <paramref name="activity"/> for this operation, in which case the telemetry item is the
        /// source of truth and its fields are copied onto the activity on dispose. False when wrapping a caller-owned activity.
        /// </param>
        public OperationHolder(TelemetryClient telemetryClient, T telemetry, Activity activity, Activity suppressedActivity, bool applyTelemetryOnDispose)
        {
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.activity = activity;
            this.suppressedActivity = suppressedActivity;
            this.applyTelemetryOnDispose = applyTelemetryOnDispose;

            // StartOperation defaults Context.Operation.Name to the operation's own name. Remember it so that, for
            // dependencies, only an explicitly assigned operation name is exported (a child dependency's own name
            // is not its operation name).
            this.initialOperationName = telemetry.Context?.Operation?.Name;
        }

        /// <summary>
        /// Gets the associated Activity for this operation, if any.
        /// </summary>
        public Activity Activity
        {
            get { return this.activity; }
        }

        /// <summary>
        /// Gets the telemetry item that represents this operation.
        /// </summary>
        public T Telemetry { get; }

        /// <summary>
        /// Disposes the operation and stops the underlying Activity, which triggers OpenTelemetry exporter emission.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            if (this.activity != null)
            {
                if (this.applyTelemetryOnDispose)
                {
                    this.ApplyTelemetryToActivity();
                }

                this.activity.Stop();
            }

            // Restore the ambient activity that was suppressed when creating a root operation
            // Only restore if the suppressed activity hasn't been stopped
            if (this.suppressedActivity != null && this.suppressedActivity.Duration == TimeSpan.Zero)
            {
                Activity.Current = this.suppressedActivity;
            }
        }

        /// <summary>
        /// Copies the telemetry item onto the activity using the same mapping as TrackRequest/TrackDependency, so fields and
        /// properties set on <see cref="Telemetry"/> during the operation are exported.
        /// </summary>
        private void ApplyTelemetryToActivity()
        {
            switch (this.Telemetry)
            {
                case DependencyTelemetry dependency:
                    bool operationNameAssigned = !string.Equals(dependency.Context?.Operation?.Name, this.initialOperationName, StringComparison.Ordinal);
                    TelemetryClient.ApplyDependencyTelemetryToActivity(dependency, this.activity, includeOperationName: operationNameAssigned);
                    break;

                case RequestTelemetry request:
                    TelemetryClient.ApplyRequestTelemetryToActivity(request, this.activity);
                    break;

                default:
                    return;
            }

            // Leave the status untouched when Success is not set, so an unset value is not reported as a failure.
            if (this.Telemetry.Success.HasValue)
            {
                this.activity.SetStatus(this.Telemetry.Success.Value ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            }
        }
    }
}
