namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    
    internal class TransmissionSender
    {
        private int transmissionCount = 0;
        private int capacity = 3;

        public event EventHandler<TransmissionProcessedEventArgs> TransmissionSent;

        /// <summary>
        /// Gets or sets the the maximum number of <see cref="Transmission"/> objects that can be sent simultaneously.
        /// </summary>
        /// <remarks>
        /// Use this property to limit the number of concurrent HTTP connections. Once the maximum number of 
        /// transmissions in progress is reached, <see cref="Enqueue"/> will stop accepting new transmissions
        /// until previous transmissions are sent.
        /// </remarks>
        public virtual int Capacity
        {
            get
            {
                return this.capacity;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                this.capacity = value;
            }
        }

        public virtual bool Enqueue(Func<Transmission> transmissionGetter)
        {
            bool enqueueSucceded = false;

            int currentCount = Interlocked.Increment(ref this.transmissionCount);
            if (currentCount <= this.capacity)
            {
                var transmission = transmissionGetter();

                if (transmission != null)
                {
                    ExceptionHandler.Start(this.StartSending, transmission);
                    enqueueSucceded = true;
                }
                else
                {
                    // We tried to dequeue from buffer and buffer is empty
                    // return false to stop moving from buffer to sender when policy is applied otherwise we get infinite loop
                }
            }
            else
            {
                TelemetryChannelEventSource.Log.SenderEnqueueNoCapacityWarning(currentCount - 1, this.capacity);
            }

            if (!enqueueSucceded)
            {
                Interlocked.Decrement(ref this.transmissionCount);
            }

            return enqueueSucceded;
        }

        protected void OnTransmissionSent(TransmissionProcessedEventArgs args)
        {
            EventHandler<TransmissionProcessedEventArgs> handler = this.TransmissionSent;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private async Task StartSending(Transmission transmission)
        {
            Exception exception = null;
            string responseContent = null;

            try
            {
                TelemetryChannelEventSource.Log.TransmissionSendStarted(transmission.Id);
                responseContent = await transmission.SendAsync().ConfigureAwait(false);          
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                int currentCapacity = Interlocked.Decrement(ref this.transmissionCount);
                if (exception == null)
                {
                    TelemetryChannelEventSource.Log.TransmissionSentSuccessfully(transmission.Id, currentCapacity);
                }
                else
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(transmission.Id, exception.ToString());
                }

                this.OnTransmissionSent(new TransmissionProcessedEventArgs(transmission, exception, responseContent));
            }
        }
    }
}
