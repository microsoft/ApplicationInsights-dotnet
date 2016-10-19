namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class TransmissionSender
    {
        private int transmissionCount = 0;
        private int capacity = 3;

        private bool applyThrottle = true;
        private int throttleWindowInMilliseconds = 1000;
        private long currentThrottleWindowId = 0;
        private int currentItemsCount = 0;
        private int throttleLimit = 10;

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

        /// <summary>
        /// Enables a limiter on the maximum number of <see cref="ITelemetry"/> objects that can be sent in a given throttle window.
        /// Items attempted to be sent in excession of the local throttle amount will be treated the same as a backend throttle.
        /// </summary>
        public virtual bool ApplyThrottle
        {
            get
            {
                return this.applyThrottle;
            }

            set
            {
                this.applyThrottle = value;
            }
        }

        /// <summary>
        /// Set the maximum number of items that will be allowed to send in a given throttle window.
        /// </summary>
        public virtual int ThrottleLimit
        {
            get
            {
                return this.throttleLimit;
            }

            set
            {
                this.throttleLimit = value;
            }
        }

        /// <summary>
        /// Set the size of the self-limiting throttle window in milliseconds.
        /// </summary>
        public virtual int ThrottleWindow
        {
            get
            {
                return this.throttleWindowInMilliseconds;
            }

            set
            {
                this.throttleWindowInMilliseconds = value;
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
            HttpWebResponseWrapper responseContent = null;

            // Locally self-throttle this payload before we send it
            Transmission acceptedTransmission = Throttle(transmission);

            // Now that we've self-imposed a throttle, we can try to send the remaining data
            try
            {
                TelemetryChannelEventSource.Log.TransmissionSendStarted(acceptedTransmission.Id);
                responseContent = await acceptedTransmission.SendAsync().ConfigureAwait(false);          
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
                    TelemetryChannelEventSource.Log.TransmissionSentSuccessfully(acceptedTransmission.Id, currentCapacity);
                }
                else
                {
                    TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(acceptedTransmission.Id, exception.ToString());
                }

                if (responseContent == null && exception is WebException)
                {
                    HttpWebResponse response = (HttpWebResponse)((WebException)exception).Response;
                    responseContent = new HttpWebResponseWrapper()
                    {
                        StatusCode = (int)response.StatusCode,
                        StatusDescription = response.StatusDescription,
                        RetryAfterHeader = response.Headers?.Get("Retry-After")
                    };
                }

                this.OnTransmissionSent(new TransmissionProcessedEventArgs(acceptedTransmission, exception, responseContent));
            }
        }

        /// <summary>
        /// Checks if the transmission throttling policy allows for sending another request.
        /// If so, this method will add a request to the current throttle count (unless peeking)
        /// </summary>
        /// <returns>The number of events that are sendable</returns>
        private int IsTransmissionSendable(int numEvents, bool peek = false)
        {
            if (!this.applyThrottle)
            {
                return numEvents;
            }

            long throttleWindowId = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / this.ThrottleWindow);
            bool isInThrottleWindow = throttleWindowId == this.currentThrottleWindowId;
            if (isInThrottleWindow && currentItemsCount < this.ThrottleLimit)
            {
                int numAccepted = Math.Min(numEvents, this.ThrottleLimit - this.currentItemsCount);
                if (!peek)
                {
                    this.currentItemsCount += numAccepted;
                }

                return numAccepted;
            }
            else if (!isInThrottleWindow)
            {
                int numAccepted = Math.Min(numEvents, this.ThrottleLimit);
                this.currentThrottleWindowId = throttleWindowId;
                this.currentItemsCount = numAccepted;
                return numAccepted;
            }

            return 0;
        }

        private Transmission Throttle(Transmission transmission)
        {
            if (!this.ApplyThrottle)
            {
                return transmission;
            }

            int attemptedItemsCount = -1;
            int acceptedItemsCount = -1;
            Tuple<Transmission,Transmission> transmissions = transmission.Split((transmissionLength) => {
                attemptedItemsCount = transmissionLength;
                acceptedItemsCount = this.IsTransmissionSendable(transmissionLength);
                return acceptedItemsCount;
            });

            Transmission acceptedTransmission = transmissions.Item1;
            Transmission rejectedTransmission = transmissions.Item2;
            
            // Send rejected payload back for retry
            if (rejectedTransmission != null)
            {
                TelemetryChannelEventSource.Log.TransmissionThrottledWarning(this.ThrottleLimit, attemptedItemsCount, acceptedItemsCount);
                SendTransmissionThrottleRejection(rejectedTransmission);
            }
            
            return acceptedTransmission;
        }

        private void SendTransmissionThrottleRejection(Transmission rejectedTransmission)
        {
            WebException exception = new WebException(
                "Transmission was split by local throttling policy",
                null,
                System.Net.WebExceptionStatus.Success,
                null
            );
            this.OnTransmissionSent(new TransmissionProcessedEventArgs(rejectedTransmission, exception, new HttpWebResponseWrapper()
            {
                StatusCode = ResponseStatusCodes.ResponseCodeTooManyRequests,
                StatusDescription = "Internally Throttled",
                RetryAfterHeader = null
            }));
        }
    }
}
