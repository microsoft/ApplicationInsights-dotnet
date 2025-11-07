namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;

    /// <summary>
    /// Represents an ongoing telemetry operation that wraps a telemetry item and its associated Activity.
    /// In the OpenTelemetry-based shim, this class simply stops the underlying Activity on dispose.
    /// </summary>
    internal sealed class OperationHolder<T> : IOperationHolder<T> where T : OperationTelemetry
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Activity activity;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationHolder{T}"/> class.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client associated with this operation.</param>
        /// <param name="telemetry">Telemetry item created for this operation.</param>
        /// <param name="activity">Activity that represents the operation context. May be null if sampled out or no listener.</param>
        public OperationHolder(TelemetryClient telemetryClient, T telemetry, Activity activity)
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            this.telemetryClient = telemetryClient;
            this.Telemetry = telemetry;
            this.activity = activity;
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
                if (this.Telemetry is DependencyTelemetry dep)
                {
                    ActivityShimMapper.ApplyDependencyTags(this.activity, dep);
                }

                this.activity.Stop();
            }
        }
    }
}
