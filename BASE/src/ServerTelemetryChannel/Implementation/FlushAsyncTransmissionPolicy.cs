namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class FlushAsyncTransmissionPolicy : TransmissionPolicy, IDisposable
    {
        // Sender will pick transmission from storage immediately after FlushAsync.
        // This timer will prevent sender from picking transmission, immediately after FlushAsync call.
        private TimeSpan pauseSenderInSeconds = TimeSpan.FromSeconds(3);
        private TaskTimerInternal pauseTimer = new TaskTimerInternal { Delay = TimeSpan.FromSeconds(3) };

        public override void Initialize(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException(nameof(transmitter));
            }

            base.Initialize(transmitter);
            transmitter.TransmissionSent += this.HandleTransmissionSentEvent;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void HandleTransmissionSentEvent(object sender, TransmissionProcessedEventArgs e)
        {
            HttpWebResponseWrapper httpWebResponseWrapper = e.Response;
            // Runs when IAsyncFlushable.FlushAsync is called
            if (httpWebResponseWrapper?.StatusCode == ResponseStatusCodes.Success && e.Transmission.HasFlushTask)
            {
                // Moves transmission to storage when sender is out of capacity
                if (httpWebResponseWrapper.StatusDescription == "SendToDisk")
                {
                    // Move current transmission to storage
                    if (!this.Transmitter.Storage.Enqueue(() => e.Transmission))
                    {
                        TelemetryChannelEventSource.Log.TransmitterStorageSkipped(e.Transmission.Id);
                        // Move to storage has failed. Set flush task as failure.
                        // Complete TaskCompletionSource of IAsyncFlushable.FlushAsync task. 
                        e.Transmission.CompleteFlushTask(false);
                    }
                }

                // Disable sending and buffer capacity to move items from buffer to the Storage
                this.MaxSenderCapacity = 0;
                this.MaxBufferCapacity = 0;
                this.LogCapacityChanged();
                this.Apply();

                // IsEnqueueSuccess flag is set to false, when enqueue to storage fail
                if (!this.Transmitter.Storage.IsEnqueueSuccess)
                {
                    // Move to storage has failed. Set flush task as failure.
                    // Complete TaskCompletionSource of IAsyncFlushable.FlushAsync task. 
                    e.Transmission.CompleteFlushTask(false);
                }

                // Completes TaskCompletionSource of IAsyncFlushable.FlushAsync task. 
                e.Transmission.CompleteFlushTask(true);

                this.pauseTimer.Delay = this.pauseSenderInSeconds;
                this.pauseTimer.Start(
                   () =>
                   {
                       this.MaxBufferCapacity = null;
                       this.MaxSenderCapacity = null;
                       this.LogCapacityChanged();
                       this.Apply();

                       return Task.FromResult<object>(null);
                   });

                return;
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