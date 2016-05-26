namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class ErrorHandlingTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private TaskTimer pauseTimer = new TaskTimer { Delay = TimeSpan.FromSeconds(TransmissionPolicyHelpers.SlotDelayInSeconds) };

        public int ConsecutiveErrors { get; set; }

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

        protected virtual TimeSpan GetBackOffTime(NameValueCollection headers)
        {
            return TransmissionPolicyHelpers.GetBackOffTime(this.ConsecutiveErrors, headers);
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
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(
                        e.Transmission.Id, 
                        webException.Message, 
                        (int)httpWebResponse.StatusCode,
                        httpWebResponse.StatusDescription);
                    this.AdditionalVerboseTracing(httpWebResponse);

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
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, (int)HttpStatusCode.InternalServerError, null);
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

        private void AdditionalVerboseTracing(HttpWebResponse httpResponse)
        {
            // For perf reason deserialize only when verbose tracing is enabled 
            if (TelemetryChannelEventSource.Log.IsVerboseEnabled && httpResponse != null)
            {
                try
                {
                    var stream = httpResponse.GetResponseStream();
                    if (stream != null)
                    {
                        using (StreamReader content = new StreamReader(stream))
                        {
                            string response = content.ReadToEnd();

                            if (!string.IsNullOrEmpty(response))
                            {
                                BackendResponse backendResponse = TransmissionPolicyHelpers.GetBackendResponse(response);

                                if (backendResponse != null && backendResponse.Errors != null)
                                {
                                    foreach (var error in backendResponse.Errors)
                                    {
                                        if (error != null)
                                        {
                                            TelemetryChannelEventSource.Log.ItemRejectedByEndpointWarning(error.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // This code is for tracing purposes only; it cannot not throw
                }
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