namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Channel.Implementation;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class PartialSuccessTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private BackoffLogicManager backoffLogicManager;
        private TaskTimerInternal pauseTimer = new TaskTimerInternal { Delay = TimeSpan.FromSeconds(BackoffLogicManager.SlotDelayInSeconds) };

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

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs args)
        {
            if (args.Exception == null && (args.Response == null || args.Response.StatusCode == ResponseStatusCodes.Success))
            {
                // We successfully sent transmittion
                this.backoffLogicManager.ResetConsecutiveErrors();
                return;
            }

            if (args.Response != null && args.Response.StatusCode == ResponseStatusCodes.PartialSuccess)
            {
                int statusCode;
                string newTransmissions = this.ParsePartialSuccessResponse(args.Transmission, args, out statusCode);

                if (!string.IsNullOrEmpty(newTransmissions))
                {
                    this.DelayFutureProcessing(args.Response, statusCode);
                    
                    byte[] data = JsonSerializer.ConvertToByteArray(newTransmissions);
                    Transmission newTransmission = new Transmission(
                        args.Transmission.EndpointAddress,
                        data,
                        args.Transmission.ContentType,
                        args.Transmission.ContentEncoding,
                        args.Transmission.Timeout);

                    this.Transmitter.Enqueue(newTransmission);
                }
                else
                {
                    // We got 206 but there is no indication in response that something was not accepted.
                    this.backoffLogicManager.ResetConsecutiveErrors();
                }
            }
        }

        private string ParsePartialSuccessResponse(Transmission initialTransmission, TransmissionProcessedEventArgs args, out int lastStatusCode)
        {
            BackendResponse backendResponse = null;
            lastStatusCode = 206;

            if (args != null && args.Response != null)
            {
                backendResponse = this.backoffLogicManager.GetBackendResponse(args.Response.Content);
            }

            if (backendResponse == null)
            { 
                return null;
            }

            string newTransmissions = null;
            if (backendResponse.ItemsAccepted != backendResponse.ItemsReceived)
            {
                string[] items = JsonSerializer
                    .Deserialize(initialTransmission.Content)
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var error in backendResponse.Errors)
                {
                    if (error != null)
                    {
                        if (error.Index >= items.Length || error.Index < 0)
                        {
                            TelemetryChannelEventSource.Log.UnexpectedBreezeResponseWarning(items.Length, error.Index);
                            continue;
                        }

                        TelemetryChannelEventSource.Log.ItemRejectedByEndpointWarning(error.Message);

                        if (error.StatusCode == ResponseStatusCodes.RequestTimeout ||
                            error.StatusCode == ResponseStatusCodes.ServiceUnavailable ||
                            error.StatusCode == ResponseStatusCodes.InternalServerError ||
                            error.StatusCode == ResponseStatusCodes.ResponseCodeTooManyRequests ||
                            error.StatusCode == ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime)
                        {
                            if (string.IsNullOrEmpty(newTransmissions))
                            {
                                newTransmissions = items[error.Index];
                            }
                            else
                            {
                                newTransmissions += Environment.NewLine + items[error.Index];
                            }

                            // Last status code is used for tracing purposes. We will get only one out of many. Most likely they all will be the same.
                            lastStatusCode = error.StatusCode;
                        }
                    }
                }
            }

            return newTransmissions;
        }

        private void DelayFutureProcessing(HttpWebResponseWrapper response, int statusCode)
        {
            // Disable sending and buffer capacity (=EnqueueAsync will enqueue to the Storage)
            this.MaxSenderCapacity = 0;
            this.MaxBufferCapacity = 0;
            this.LogCapacityChanged();
            this.Apply();

            // Back-off for the Delay duration and enable sending capacity
            this.backoffLogicManager.ReportBackoffEnabled(statusCode);

            this.pauseTimer.Delay = this.backoffLogicManager.GetBackOffTimeInterval(response.RetryAfterHeader);
            this.pauseTimer.Start(
                () =>
                    {
                        this.MaxBufferCapacity = null;
                        this.MaxSenderCapacity = null;
                        this.LogCapacityChanged();
                        this.Apply();

                        return TaskEx.FromResult<object>(null);
                    });
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
