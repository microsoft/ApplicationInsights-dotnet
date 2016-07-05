namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel.Implementation;

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
            var webException = e.Exception as WebException;
            if (webException != null)
            {
                HttpWebResponse httpWebResponse = webException.Response as HttpWebResponse;
                if (httpWebResponse != null)
                {
                    if (httpWebResponse.StatusCode == (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequests ||
                        httpWebResponse.StatusCode == (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime)
                    {
                        this.backoffLogicManager.ConsecutiveErrors++;

                        this.MaxSenderCapacity = 0;
                        if (httpWebResponse.StatusCode == (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime)
                        {
                            // We start loosing data!
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
                        this.Transmitter.Enqueue(e.Transmission);

                        this.backoffLogicManager.ScheduleRestore(
                            httpWebResponse.Headers, 
                            () =>
                                {
                                    this.ResetPolicy();
                                    return TaskEx.FromResult<object>(null);
                                });
                    }
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
