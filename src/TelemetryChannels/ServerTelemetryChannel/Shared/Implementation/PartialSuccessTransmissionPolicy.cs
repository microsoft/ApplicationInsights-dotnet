namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading.Tasks;

    using System.Web.Script.Serialization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class PartialSuccessTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

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
            BackendResponse backendResponse;
            string responseContent = args.Response.Content;
            try
            {
                backendResponse = this.serializer.Deserialize<BackendResponse>(responseContent);
            }
            catch (ArgumentException exp)
            {
                TelemetryChannelEventSource.Log.BreezeResponseWasNotParsedWarning(exp.Message, responseContent);
                this.ConsecutiveErrors = 0;
                return null;
            }
            catch (InvalidOperationException exp)
            {
                TelemetryChannelEventSource.Log.BreezeResponseWasNotParsedWarning(exp.Message, responseContent);
                this.ConsecutiveErrors = 0;
                return null;
            }

            string newTransmissions = null;

            if (backendResponse != null && backendResponse.ItemsAccepted != backendResponse.ItemsReceived)
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
            this.pauseTimer.Delay = this.GetBackOffTime(response.RetryAfterHeader);
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
