namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Channel.Implementation;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class PartialSuccessTransmissionPolicy : TransmissionPolicy, IDisposable
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

        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs args)
        {
            if (args.Response != null && args.Response.StatusCode == ResponseStatusCodes.PartialSuccess)
            {
                string newTransmissions = this.ParsePartialSuccessResponse(args.Transmission, args);

                if (!string.IsNullOrEmpty(newTransmissions))
                {
                    this.ConsecutiveErrors++;
                    this.DelayFutureProcessing(args.Response);
                    
                    byte[] data = JsonSerializer.ConvertToByteArray(newTransmissions);
                    Transmission newTransmission = new Transmission(
                        args.Transmission.EndpointAddress,
                        data,
                        args.Transmission.ContentType,
                        args.Transmission.ContentEncoding,
                        args.Transmission.Timeout);

                    this.Transmitter.Enqueue(newTransmission);
                }
            }
        }

        private string ParsePartialSuccessResponse(Transmission initialTransmission, TransmissionProcessedEventArgs args)
        {
            BackendResponse backendResponse = null;

            if (args != null && args.Response != null)
            {
                backendResponse = TransmissionPolicyHelpers.GetBackendResponse(args.Response.Content);
            }

            if (backendResponse == null)
            { 
                this.ConsecutiveErrors = 0;
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
                        }
                    }
                }
            }

            return newTransmissions;
        }

        private void DelayFutureProcessing(HttpWebResponseWrapper response)
        {
            // Disable sending and buffer capacity (=EnqueueAsync will enqueue to the Storage)
            this.MaxSenderCapacity = 0;
            this.MaxBufferCapacity = 0;
            this.LogCapacityChanged();
            this.Apply();

            // Back-off for the Delay duration and enable sending capacity
            this.pauseTimer.Delay = TransmissionPolicyHelpers.GetBackOffTime(this.ConsecutiveErrors, response.RetryAfterHeader);
            this.pauseTimer.Start(() =>
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
