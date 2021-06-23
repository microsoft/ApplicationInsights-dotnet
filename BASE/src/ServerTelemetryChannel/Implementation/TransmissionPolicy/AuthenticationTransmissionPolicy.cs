namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation.TransmissionPolicy
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class AuthenticationTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private BackoffLogicManager backoffLogicManager;
        private TaskTimerInternal pauseTimer = new TaskTimerInternal { Delay = TimeSpan.FromSeconds(BackoffLogicManager.SlotDelayInSeconds) };

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

        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs e)
        {
            if (this.Enabled)
            {
                HttpWebResponseWrapper httpWebResponseWrapper = e.Response;
                if (httpWebResponseWrapper != null)
                {
                    if (httpWebResponseWrapper.StatusCode == ResponseStatusCodes.BadRequest)
                    {
                        // HTTP/1.1 400 Incorrect API was used - v2 API does not support authentication.
                        // This indicates that the AI resource was configured for AAD, but SDK was not.
                        // This is a configuration issue and is not recoverable. 
                        this.ApplyHaltPolicy(e);
                    }
                    else if (httpWebResponseWrapper.StatusCode == ResponseStatusCodes.Unauthorized)
                    {
                        // HTTP/1.1 401 Unauthorized - please provide the valid authorization token
                        // This indicates that the authorization token was either absent, invalid, or expired.
                        // The root cause is not known and we should throttle retries.
                        this.ApplyThrottlePolicy(e);
                    }
                    else if (httpWebResponseWrapper.StatusCode == ResponseStatusCodes.Forbidden)
                    {
                        // HTTP/1.1 403 Forbidden - provided credentials do not grant the access to ingest the telemetry into the component
                        // This indicates the configured identity does not have permissions to publish to this resource.
                        // This can be recovered if the user changes the AI Resource configuration.
                        this.ApplyThrottlePolicy(e);
                    }
                }
            }
        }

        private void ApplyHaltPolicy(TransmissionProcessedEventArgs e)
        {
            this.MaxSenderCapacity = 0;
            this.MaxBufferCapacity = 0;
            this.MaxStorageCapacity = null;

            this.LogCapacityChanged();
            this.Apply();

            this.backoffLogicManager.ReportBackoffEnabled(e.Response.StatusCode);
            this.Transmitter.Enqueue(e.Transmission);
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

            // Ingestion service does not provide a retry value. We use our own here.
            this.pauseTimer.Delay = TimeSpan.FromSeconds(30);
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
