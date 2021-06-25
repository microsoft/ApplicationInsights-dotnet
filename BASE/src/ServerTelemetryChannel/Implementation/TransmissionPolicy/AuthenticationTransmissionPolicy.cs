namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation.TransmissionPolicy
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// This class defines how the ServerTelemetryChannel will behave when it receives Response Codes 
    /// from the Ingestion Service related to Authentication (AAD) scenarios.
    /// </summary>
    /// <remarks>
    /// This class is disabled by default and expected to be enabled only when AAD has been configured in the AI SDK.
    /// </remarks>
    internal class AuthenticationTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private BackoffLogicManager backoffLogicManager;
        private TaskTimerInternal pauseTimer = new TaskTimerInternal { Delay = TimeSpan.FromMinutes(1) };

        public bool Enabled { get; set; } = false;

        public override void Initialize(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException(nameof(transmitter));
            }

            this.backoffLogicManager = transmitter.BackoffLogicManager;

            base.Initialize(transmitter);
            transmitter.TransmissionSent += this.HandleTransmissionSentEvent;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This method subscribes to the <see cref="Transmitter.TransmissionSent"/> event.
        /// This encapsulates all <see cref="HttpWebResponseWrapper.StatusCode"/> related to Authentication (AAD) scenarios.
        /// </summary>
        /// <remarks>
        /// AN EXPLANATION OF THE STATUS CODES:
        /// - <see cref="ResponseStatusCodes.BadRequest"/>
        /// "HTTP/1.1 400 Incorrect API was used - v2 API does not support authentication".
        /// This indicates that the AI resource was configured for AAD, but SDK was not.
        /// This is a configuration issue and is not recoverable from the client side. 
        /// If the customer chooses to disable AAD in the AI Resource, the SDK could resume sending.
        /// - <see cref="ResponseStatusCodes.Unauthorized"/>
        /// "HTTP/1.1 401 Unauthorized - please provide the valid authorization token".
        /// This indicates that the authorization token was either absent, invalid, or expired.
        /// The root cause is not known and we should throttle retries.
        /// - <see cref="ResponseStatusCodes.Forbidden"/>
        /// "HTTP/1.1 403 Forbidden - provided credentials do not grant the access to ingest the telemetry into the component".
        /// This indicates the configured identity does not have permissions to publish to this resource.
        /// This is a configuration issue and is not recoverable from the client side. 
        /// This can be recovered if the user changes the AI Resource's configured Access Control.
        /// </remarks>
        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs e)
        {
            if (this.Enabled && e.Response != null)
            {
                switch (e.Response.StatusCode)
                {
                    case ResponseStatusCodes.BadRequest:
                    case ResponseStatusCodes.Unauthorized:
                    case ResponseStatusCodes.Forbidden:
                        this.ApplyThrottlePolicy(e);
                        break;
                }
            }
        }

        private void ApplyThrottlePolicy(TransmissionProcessedEventArgs e)
        {
            this.MaxSenderCapacity = 0;
            this.MaxBufferCapacity = null;
            this.MaxStorageCapacity = null;

            this.LogCapacityChanged();
            this.Apply();

            this.backoffLogicManager.ReportBackoffEnabled(e.Response.StatusCode);
            this.Transmitter.Enqueue(e.Transmission);

            // Ingestion service does not provide a retry value for these scenarios.
            // Check this.pauseTimer above for the configured wait time.
            this.pauseTimer.Start(() =>
                {
                    this.ResetPolicy();
                    return Task.FromResult<object>(null);
                });
        }

        private void ResetPolicy()
        {
            this.MaxSenderCapacity = null;
            this.MaxBufferCapacity = null;
            this.MaxStorageCapacity = null;
            this.LogCapacityChanged();
            this.Apply();
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.pauseTimer != null)
                {
                    this.pauseTimer.Dispose();
                    this.pauseTimer = null;
                }
            }
        }
    }
}
