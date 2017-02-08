namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using Extensibility.Implementation.Tracing;

    /// <summary>
    /// Operation class that holds the telemetry item and the corresponding telemetry client.
    /// </summary>
    internal class AsyncLocalBasedOperationHolder<T> : IOperationHolder<T> where T : OperationTelemetry
    {
        /// <summary>
        /// Parent context store that is used to restore call context.
        /// </summary>
        public OperationContextForAsyncLocal ParentContext;

        private TelemetryClient telemetryClient;
        private T telemetry;

        /// <summary>
        /// Indicates if this instance has been disposed of.
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLocalBasedOperationHolder{T}"/> class.
        /// Initializes telemetry client.
        /// </summary>
        /// <param name="telemetryClient">Initializes telemetry client object.</param>
        /// <param name="telemetry">Operation telemetry item that is assigned to the telemetry associated to the current operation item.</param>
        public AsyncLocalBasedOperationHolder(TelemetryClient telemetryClient, T telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException("telemetry");
            }

            if (telemetryClient == null)
            {
                throw new ArgumentNullException("telemetryClient");
            }

            this.telemetryClient = telemetryClient;
            this.telemetry = telemetry;
        }

        /// <summary>
        /// Gets Telemetry item of interest that is created when StartOperation function of ClientExtensions is invoked.
        /// </summary>
        public T Telemetry
        {
            get { return this.telemetry; }
        }

        /// <summary>
        /// Dispose method to clear the variables.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Computes the duration and tracks the respective telemetry item on dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.isDisposed)
            {
                // We need to compare the operation id and name of telemetry with opeartion id and name of current call context before tracking it 
                // to make sure that the customer is tracking the right telemetry.
                lock (this)
                {
                    if (!this.isDisposed)
                    {
                        var operationTelemetry = this.Telemetry as OperationTelemetry;

                        var currentOperationContext = AsyncLocalHelpers.GetCurrentOperationContext();
                        if (operationTelemetry.Id != currentOperationContext.ParentOperationId ||
                            operationTelemetry.Context.Operation.Name != currentOperationContext.RootOperationName)
                        {
                            CoreEventSource.Log.InvalidOperationToStopError();
                            return;
                        }

                        operationTelemetry.Stop();
                        AsyncLocalHelpers.RestoreOperationContext(this.ParentContext);
                        this.telemetryClient.Track(operationTelemetry);
                    }

                    this.isDisposed = true;
                }
            }
        }
    }
}
