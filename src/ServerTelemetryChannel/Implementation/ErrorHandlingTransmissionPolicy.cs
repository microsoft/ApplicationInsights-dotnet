using System.Globalization;

namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using ApplicationInsights.Channel.Implementation;
    using Extensibility.Implementation;

    internal class ErrorHandlingTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        private BackoffLogicManager backoffLogicManager;
        private TaskTimerInternal pauseTimer = new TaskTimerInternal { Delay = TimeSpan.FromSeconds(BackoffLogicManager.SlotDelayInSeconds) };

        public override void Initialize(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException(nameof(transmitter));
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

        private static void AdditionalVerboseTracing(string httpResponse)
        {
            // For perf reason deserialize the response only when verbose tracing is enabled 
            if (TelemetryChannelEventSource.IsVerboseEnabled && httpResponse != null)
            {
                try
                {
                    BackendResponse backendResponse = BackoffLogicManager.GetBackendResponse(httpResponse);
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
                catch (Exception)
                {
                    // This code is for tracing purposes only; it cannot not throw
                }
            }
        }

        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs e)
        {
            HttpWebResponseWrapper httpWebResponseWrapper = e.Response;
            if (httpWebResponseWrapper != null)
            {
                AdditionalVerboseTracing(httpWebResponseWrapper.Content);
                switch (httpWebResponseWrapper.StatusCode)
                {
                    case ResponseStatusCodes.BadGateway:
                    case ResponseStatusCodes.GatewayTimeout:
                    case ResponseStatusCodes.RequestTimeout:
                    case ResponseStatusCodes.ServiceUnavailable:
                    case ResponseStatusCodes.InternalServerError:
                    case ResponseStatusCodes.UnknownNetworkError:
                        // Disable sending and buffer capacity (Enqueue will enqueue to the Storage)
                        this.MaxSenderCapacity = 0;
                        this.MaxBufferCapacity = 0;
                        this.LogCapacityChanged();
                        this.Apply();

                        this.backoffLogicManager.ReportBackoffEnabled((int)httpWebResponseWrapper.StatusCode);
                        this.Transmitter.Enqueue(e.Transmission);

                        this.pauseTimer.Delay = this.backoffLogicManager.GetBackOffTimeInterval(httpWebResponseWrapper.RetryAfterHeader);
                        this.pauseTimer.Start(
                           () =>
                           {
                               this.MaxBufferCapacity = null;
                               this.MaxSenderCapacity = null;
                               this.LogCapacityChanged();
                               this.Apply();

                               this.backoffLogicManager.ReportBackoffDisabled();

                               return Task.FromResult<object>(null);
                           });
                        break;
                    default:                        
                        // We are loosing data here but that is intentional as the response code is
                        // not in the whitelisted set to attempt retry.
                        TelemetryChannelEventSource.Log.TransmissionDataNotRetriedForNonWhitelistedResponse(e.Transmission.Id,
                            httpWebResponseWrapper.StatusCode.ToString(CultureInfo.InvariantCulture));
                        break;
                }
            }
            else
            {
                // Data loss Unknown Exception
                // We are loosing data here (we did not upload failed transaction back).
                // We got unknown exception. 
                if (e.Exception != null)
                {                    
                    TelemetryChannelEventSource.Log.TransmissionDataLossError(e.Transmission.Id,
                        e.Exception.Message);
                }
                else
                {
                    TelemetryChannelEventSource.Log.TransmissionDataLossError(e.Transmission.Id,
                        "Unknown Exception Message");
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