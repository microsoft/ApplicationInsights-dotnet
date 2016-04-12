namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class ErrorHandlingTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private TaskTimer pauseTimer = new TaskTimer { Delay = TimeSpan.FromSeconds(SlotDelayInSeconds) };

        public override void Initialize(Transmitter transmitter)
        {
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
            var webException = e.Exception as WebException;
            if (webException != null)
            {
                this.ConsecutiveErrors++;
                HttpWebResponse httpWebResponse = webException.Response as HttpWebResponse;
                if (httpWebResponse != null)
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, (int)httpWebResponse.StatusCode);

                    switch (httpWebResponse.StatusCode)
                    {
                        case (HttpStatusCode)408:
                        case (HttpStatusCode)503:
                        case (HttpStatusCode)500:
                            // Disable sending and buffer capacity (=EnqueueAsync will enqueue to the Storage)
                            this.MaxSenderCapacity = 0;
                            this.MaxBufferCapacity = 0;
                            this.LogCapacityChanged();
                            this.Apply();

                            // Back-off for the Delay duration and enable sending capacity
                            this.pauseTimer.Delay = this.GetBackOffTime(httpWebResponse.Headers);
                            this.pauseTimer.Start(() =>
                            {
                                this.MaxBufferCapacity = null;
                                this.MaxSenderCapacity = null;
                                this.LogCapacityChanged();
                                this.Apply();

                                return TaskEx.FromResult<object>(null);
                            });

                            this.Transmitter.Enqueue(e.Transmission);
                            break;
                    }
                }
                else
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, (int)HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                if (e.Exception != null)
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(e.Transmission.Id, e.Exception.Message);
                }

                this.ConsecutiveErrors = 0;
            }
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