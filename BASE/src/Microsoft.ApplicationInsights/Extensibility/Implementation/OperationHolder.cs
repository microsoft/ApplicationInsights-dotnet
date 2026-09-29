namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Represents an ongoing telemetry operation that wraps a telemetry item and its associated Activity.
    /// In the OpenTelemetry-based shim, disposing copies the telemetry item onto the Activity and stops the Activity
    /// so that the exporter emits it.
    /// </summary>
    internal sealed class OperationHolder<T> : IOperationHolder<T> where T : OperationTelemetry
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Activity activity;
        private readonly Activity suppressedActivity;
        private readonly bool ownsActivity;
        private readonly string initialName;
        private readonly string defaultOperationName;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationHolder{T}"/> class for a caller-owned activity.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client associated with this operation.</param>
        /// <param name="telemetry">Telemetry item created for this operation.</param>
        /// <param name="activity">Activity that represents the operation context. May be null if sampled out or no listener.</param>
        public OperationHolder(TelemetryClient telemetryClient, T telemetry, Activity activity)
            : this(telemetryClient, telemetry, activity, null, ownsActivity: false, defaultOperationName: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationHolder{T}"/> class.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client associated with this operation.</param>
        /// <param name="telemetry">Telemetry item created for this operation.</param>
        /// <param name="activity">Activity that represents the operation context. May be null if sampled out or no listener.</param>
        /// <param name="suppressedActivity">An ambient activity that was suppressed to create a root operation and should be restored on dispose.</param>
        /// <param name="ownsActivity">
        /// True when the SDK created <paramref name="activity"/> for this operation: the telemetry item is the source of truth and
        /// is fully copied onto the activity on dispose. False when wrapping a caller-owned activity: only fields the caller
        /// explicitly set on the telemetry item are applied, so the activity's own instrumentation is not overwritten.
        /// </param>
        /// <param name="defaultOperationName">
        /// The value the SDK assigned to <c>Context.Operation.Name</c> as a default (the operation's own name), or null if the
        /// caller provided it. For dependencies, the operation name is only exported when it differs from this default, since a
        /// child dependency's own name is not its operation name.
        /// </param>
        public OperationHolder(TelemetryClient telemetryClient, T telemetry, Activity activity, Activity suppressedActivity, bool ownsActivity, string defaultOperationName)
        {
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.activity = activity;
            this.suppressedActivity = suppressedActivity;
            this.ownsActivity = ownsActivity;
            this.initialName = telemetry.Name;
            this.defaultOperationName = defaultOperationName;
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
                this.ApplyTelemetryToActivity();
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
            bool nameChanged = !string.Equals(this.Telemetry.Name, this.initialName, StringComparison.Ordinal);

            switch (this.Telemetry)
            {
                case DependencyTelemetry dependency when this.ownsActivity:
                    string operationName = dependency.Context?.Operation?.Name;
                    bool operationNameAssigned = !string.IsNullOrEmpty(operationName) && !string.Equals(operationName, this.defaultOperationName, StringComparison.Ordinal);
                    TelemetryClient.ApplyDependencyTelemetryToActivity(dependency, this.activity, includeOperationName: operationNameAssigned);
                    break;

                case DependencyTelemetry dependency:
                    TelemetryClient.ApplyDependencyOverrideAttributes(dependency, this.activity, includeName: nameChanged);
                    break;

                case RequestTelemetry request when this.ownsActivity:
                    TelemetryClient.ApplyRequestTelemetryToActivity(request, this.activity);
                    break;

                case RequestTelemetry request:
                    TelemetryClient.ApplyRequestOverrideAttributes(request, this.activity, includeName: nameChanged);
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
