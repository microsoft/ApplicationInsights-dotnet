namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class ThrottlingTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private TaskTimer pauseTimer = new TaskTimer { Delay = TimeSpan.FromSeconds(TransmissionPolicyHelpers.SlotDelayInSeconds) };

        public int ConsecutiveErrors { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override void Initialize(Transmitter transmitter)
        {
            base.Initialize(transmitter);
            transmitter.TransmissionSent += this.HandleTransmissionSentEvent;
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
                    if (httpWebResponse.StatusCode == (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequests ||
                        httpWebResponse.StatusCode == (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime)
                    {
                        this.pauseTimer.Delay = TransmissionPolicyHelpers.GetBackOffTime(this.ConsecutiveErrors, httpWebResponse.Headers);
                        TelemetryChannelEventSource.Log.ThrottlingRetryAfterParsedInSec(this.pauseTimer.Delay.TotalSeconds);

                        this.MaxSenderCapacity = 0;
                        this.MaxBufferCapacity =
                            httpWebResponse.StatusCode ==
                            (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime ? (int?)0 : null;
                        this.MaxStorageCapacity =
                            httpWebResponse.StatusCode ==
                            (HttpStatusCode)ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime ? (int?)0 : null;

                        this.LogCapacityChanged();

                        this.Apply();

                        // Back-off for the Delay duration and enable sending capacity
                        this.pauseTimer.Start(() =>
                        {
                            this.ResetPolicy();
                            return TaskEx.FromResult<object>(null);
                        });

                        this.Transmitter.Enqueue(e.Transmission);
                    }
                }
            }
            else
            {
                this.ConsecutiveErrors = 0;
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
