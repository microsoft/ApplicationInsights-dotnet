namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    using ApplicationInsights.Channel.Implementation;
    using Extensibility.Implementation;

#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class ErrorHandlingTransmissionPolicy : TransmissionPolicy
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
                HttpWebResponseWrapper httpWebResponse = e.Response;
                if (httpWebResponse != null)
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(
                        e.Transmission.Id, 
                        webException.Message, 
                        (int)httpWebResponse.StatusCode,
                        httpWebResponse.StatusDescription);

                    this.AdditionalVerboseTracing((HttpWebResponse)webException.Response);

                    switch (httpWebResponse.StatusCode)
                    {
                        case ResponseStatusCodes.RequestTimeout:
                        case ResponseStatusCodes.ServiceUnavailable:
                        case ResponseStatusCodes.InternalServerError:
                            // Disable sending and buffer capacity (Enqueue will enqueue to the Storage)
                            this.MaxSenderCapacity = 0;
                            this.MaxBufferCapacity = 0;
                            this.LogCapacityChanged();
                            this.Apply();

                            this.backoffLogicManager.ReportBackoffEnabled((int)httpWebResponse.StatusCode);
                            this.Transmitter.Enqueue(e.Transmission);

                            this.backoffLogicManager.ScheduleRestore(
                               httpWebResponse.RetryAfterHeader,
                               () =>
                                    {
                                        this.MaxBufferCapacity = null;
                                        this.MaxSenderCapacity = null;
                                        this.LogCapacityChanged();
                                        this.Apply();

                                        this.backoffLogicManager.ReportBackoffDisabled();

                                        return TaskEx.FromResult<object>(null);
                                    });
                            break;
                    }
                }
                else
                {
                    // We are loosing data here (we did not upload failed transaction back).
                    // We did not get response back. 
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, (int)HttpStatusCode.InternalServerError, null);
                }
            }
            else
            {
                if (e.Exception != null)
                {
                    // We are loosing data here (we did not upload failed transaction back).
                    // We got unknown exception. 
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(e.Transmission.Id, e.Exception.Message);
                }
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
                                BackendResponse backendResponse = this.backoffLogicManager.GetBackendResponse(response);

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
    }
}