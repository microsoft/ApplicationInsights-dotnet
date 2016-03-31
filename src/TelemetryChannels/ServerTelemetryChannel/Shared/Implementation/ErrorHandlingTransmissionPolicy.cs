namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using System.Web.Script.Serialization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    
#if NET45
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class ErrorHandlingTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private const int ResponseCodePaymentRequired = 402;
        private const int RequestTimeout = 408;
        private const int ResponseCodeTooManyRequests = 429;
        private const int ResponseCodeTooManyRequestsOverExtendedTime = 439;
        private const int InternalServerError = 500;
        private const int ServiceUnavailable = 503;

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
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, (int)httpWebResponse.StatusCode);

                    this.HandleStatusCode((int)httpWebResponse.StatusCode, e.Transmission);
                }
                else
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWebExceptionWarning(e.Transmission.Id, webException.Message, (int)HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(e.ResponseContent))
                {
                    // Partial success case (206)
                    var newTransmissions = this.ParsePartialSuccessResponse(e.Transmission, e.ResponseContent);

                    if (newTransmissions != null)
                    {
                        foreach (int statusCode in newTransmissions.Keys)
                        {
                            byte[] data = JsonSerializer.ConvertToByteArray(newTransmissions[statusCode]);
                            Transmission newTransmission = new Transmission(
                                e.Transmission.EndpointAddress,
                                data,
                                e.Transmission.ContentType,
                                e.Transmission.ContentEncoding,
                                e.Transmission.Timeout);

                            this.HandleStatusCode(statusCode, newTransmission);
                        }
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
        }

        private IDictionary<int, string> ParsePartialSuccessResponse(Transmission initialTransmission, string response)
        {
            BreezeResponse breezeResponse;
            try
            {
                breezeResponse = new JavaScriptSerializer().Deserialize<BreezeResponse>(response);
            }
            catch (ArgumentException exp)
            {
                TelemetryChannelEventSource.Log.BreezeResponseWasNotParsedWarning(exp.Message, response);
                this.ConsecutiveErrors = 0;
                return null;
            }
            catch (InvalidOperationException exp)
            {
                TelemetryChannelEventSource.Log.BreezeResponseWasNotParsedWarning(exp.Message, response);
                this.ConsecutiveErrors = 0;
                return null;
            }

            IDictionary<int, string> newTransmissions = null;

            if (breezeResponse != null && breezeResponse.ItemsAccepted != breezeResponse.ItemsReceived)
            {
                string[] items = JsonSerializer
                    .Deserialize(initialTransmission.Content)
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                newTransmissions = new Dictionary<int, string>();

                foreach (var error in breezeResponse.Errors)
                {
                    if (error != null)
                    {
                        if (error.Index >= items.Length)
                        {
                            TelemetryChannelEventSource.Log.UnexpectedBreezeResponseWarning(items.Length, error.Index);
                            continue;
                        }

                        TelemetryChannelEventSource.Log.ItemRejectedByEndpointWarning(error.Message);
                        
                        if (!newTransmissions.ContainsKey(error.StatusCode))
                        {
                            newTransmissions.Add(error.StatusCode, items[error.Index]);
                        }
                        else
                        {
                            string transmissions = newTransmissions[error.StatusCode];
                            newTransmissions[error.StatusCode] = transmissions + Environment.NewLine + items[error.Index];
                        }
                    }
                }

                if (newTransmissions.Count > 0)
                {
                    this.ConsecutiveErrors++;
                }
            }

            return newTransmissions;
        }

        private void HandleStatusCode(int statusCode, Transmission transmission)
        {
            switch (statusCode)
            {
                case RequestTimeout:
                case ServiceUnavailable:
                case InternalServerError:
                case ResponseCodeTooManyRequests:
                case ResponseCodeTooManyRequestsOverExtendedTime:
                case ResponseCodePaymentRequired:
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

                    this.Transmitter.Enqueue(transmission);
                    break;
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
