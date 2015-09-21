namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Net;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class ThrottlingTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private const int ResponseCodeTooManyRequests = 429;
        private const int ResponseCodeTooManyRequestsOverExtendedTime = 439;
        private const int ResponseCodePaymentRequired = 402;

        private readonly TaskTimer pauseTimer = new TaskTimer();

        /// <summary>
        /// Gets a value that determines amount of time transmission sending will
        /// be paused before attempting to resume transmission after a network error is detected.
        /// </summary>
        public TimeSpan PauseDuration
        {
            get { return this.pauseTimer.Delay; }
        }

        public void Dispose()
        {
            this.pauseTimer.Dispose();
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
                HttpWebResponse httpWebResponse = webException.Response as HttpWebResponse;
                if (httpWebResponse != null)
                {
                    if (httpWebResponse.StatusCode == (HttpStatusCode)ResponseCodeTooManyRequests ||
                        httpWebResponse.StatusCode == (HttpStatusCode)ResponseCodeTooManyRequestsOverExtendedTime ||
                        httpWebResponse.StatusCode == (HttpStatusCode)ResponseCodePaymentRequired)
                    {
                        TimeSpan retryAfterTimeSpan;
                        if (!this.TryParseRetryAfter(httpWebResponse.Headers, out retryAfterTimeSpan))
                        {
                            this.ResetPolicy();                         
                        }
                        else
                        {
                            this.pauseTimer.Delay = retryAfterTimeSpan;
                            TelemetryChannelEventSource.Log.ThrottlingRetryAfterParsedInSec(retryAfterTimeSpan.TotalSeconds);

                            this.MaxSenderCapacity = 0;
                            this.MaxBufferCapacity =
                                httpWebResponse.StatusCode == (HttpStatusCode)ResponseCodeTooManyRequestsOverExtendedTime ?
                                (int?)0 :
                                null;
                            this.MaxStorageCapacity =
                                httpWebResponse.StatusCode == (HttpStatusCode)ResponseCodeTooManyRequestsOverExtendedTime ?
                                (int?)0 :
                                null;

                            this.LogCapacityChanged();

                            this.Apply();

                            // Back-off for the Delay duration and enable sending capacity
                            this.pauseTimer.Start(() =>
                            {
                                this.ResetPolicy();
                                return null;
                            });

                            this.Transmitter.Enqueue(e.Transmission);
                        }
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

        private bool TryParseRetryAfter(WebHeaderCollection headers, out TimeSpan retryAfterTimeSpan)
        {
            retryAfterTimeSpan = TimeSpan.FromSeconds(0);
            var retryAfter = headers.Get("Retry-After");
            if (retryAfter == null)
            {
                return false;
            }

            var now = DateTime.Now;
            DateTime retryAfterDate;
            if (DateTime.TryParse(retryAfter, out retryAfterDate))
            {
                if (retryAfterDate > now)
                {
                    retryAfterTimeSpan = retryAfterDate - now;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            TelemetryChannelEventSource.Log.TransmissionPolicyRetryAfterParseFailedWarning(retryAfter);

            return false;
        }
    }
}
