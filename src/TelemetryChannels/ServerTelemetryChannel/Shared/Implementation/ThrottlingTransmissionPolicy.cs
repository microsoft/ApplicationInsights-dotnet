namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class ThrottlingTransmissionPolicy : TransmissionPolicy
    {
        private BackoffLogicManager backoffLogicManager;

        public override void Initialize(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException();
            }

            this.backoffLogicManager = transmitter.BackoffLogicManager;

            base.Initialize(transmitter);
            transmitter.TransmissionSent += this.HandleTransmissionSentEvent;
        }

        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs e)
        {
            HttpWebResponseWrapper httpWebResponse = e.Response;
            if (httpWebResponse != null)
            {
                if (httpWebResponse.StatusCode == ResponseStatusCodes.ResponseCodeTooManyRequests ||
                    httpWebResponse.StatusCode == ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime)
                {
                    this.MaxSenderCapacity = 0;
                    if (httpWebResponse.StatusCode == ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime)
                    {
                        // We start losing data!
                        this.MaxBufferCapacity = 0;
                        this.MaxStorageCapacity = 0;
                    }
                    else
                    {
                        this.MaxBufferCapacity = null;
                        this.MaxStorageCapacity = null;
                    }

                    this.LogCapacityChanged();
                    this.Apply();

                    this.backoffLogicManager.ReportBackoffEnabled((int)httpWebResponse.StatusCode);
                    this.Transmitter.Enqueue(e.Transmission);

                    this.backoffLogicManager.ScheduleRestore(
                        httpWebResponse.RetryAfterHeader, 
                        () =>
                            {
                                this.ResetPolicy();
                                return TaskEx.FromResult<object>(null);
                            });
                }
            }
        }

        private void ResetPolicy()
        {
            this.MaxSenderCapacity = null;
            this.MaxBufferCapacity = null;
            this.MaxStorageCapacity = null;
            this.LogCapacityChanged();
            this.Apply();     
        }
    }
}
