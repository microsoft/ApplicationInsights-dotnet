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
        private const int SlotDelayInSeconds = 10;
        private const int MaxDelayInSeconds = 3600;
        private readonly Random random = new Random();
        private TaskTimer pauseTimer = new TaskTimer { Delay = TimeSpan.FromSeconds(SlotDelayInSeconds) };
        
        internal int ConsecutiveErrors { get; set; }

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

        // Calculates the time to wait before retrying in case of an error based on
        // http://en.wikipedia.org/wiki/Exponential_backoff
        internal virtual TimeSpan GetBackOffTime()
        {
            double delayInSeconds;

            if (this.ConsecutiveErrors <= 1)
            {
                delayInSeconds = SlotDelayInSeconds;
            }
            else
            {
                double backOffSlot = (Math.Pow(2, this.ConsecutiveErrors) - 1) / 2;
                var backOffDelay = this.random.Next(1, (int)Math.Min(backOffSlot * SlotDelayInSeconds, int.MaxValue));
                delayInSeconds = Math.Max(Math.Min(backOffDelay, MaxDelayInSeconds), SlotDelayInSeconds);
            }

            TelemetryChannelEventSource.Log.BackoffTimeSetInSeconds(delayInSeconds);
            return TimeSpan.FromSeconds(delayInSeconds);
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
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, httpWebResponse.StatusCode);
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
                            this.pauseTimer.Delay = this.GetBackOffTime();
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
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                var theException = e.Exception as Exception;
                if (null != theException)
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(e.Transmission.Id, theException.Message);
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
