namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Common.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class TransmissionSender
    {      
        // private static string transmissionBatchId = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
        private readonly object lockObj = new object();
        // Stores all inflight requests with a common id, before SendAsync.
        // Removes entry from dictionary after getting response.
        // Changes common id with IAsyncFlushable.FlushAsync, so it starts next batch to track for next IAsyncFlushable.FlushAsync.
        // private ConcurrentDictionary<Task<HttpWebResponseWrapper>, string> inTransmitIds = new ConcurrentDictionary<Task<HttpWebResponseWrapper>, string>();
        private List<InFlightTransmission> inFlightTransmissions = new List<InFlightTransmission>();
        private int transmissionCount = 0;
        private int capacity = 10;

        private bool applyThrottle = false;
        private int throttleWindowInMilliseconds = 1000;
        private long currentThrottleWindowId = 0;
        private int currentItemsCount = 0;
        private int throttleLimit = 10;

        public event EventHandler<TransmissionProcessedEventArgs> TransmissionSent;

        /// <summary>
        /// Gets or sets the maximum number of <see cref="Transmission"/> objects that can be sent simultaneously.
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
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.capacity = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a limiter on the maximum number of <see cref="ITelemetry"/> objects 
        /// that can be sent in a given throttle window is enabled. Items attempted to be sent exceeding of the local 
        /// throttle amount will be treated the same as a backend throttle.
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
        /// Gets or sets the maximum number of items that will be allowed to send in a given throttle window.
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
        /// Gets or sets the size of the self-limiting throttle window in milliseconds.
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
            var transmission = transmissionGetter();

            if (currentCount <= this.capacity)
            {
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

                if (transmission?.HasFlushTask == true)
                {                   
                    /* string lastTransmissionBatchId = transmissionBatchId;
                    lock (this.lockObj)
                    {
                        // Generate new transmissionBatchId for next transmissions to be ready for next IAsyncFlushable.FlushAsync (if called).
                        transmissionBatchId = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
                    }*/

                    // Wait for inflight transmissions to complete.
                    // this.WaitForPreviousTransmissionsToComplete(transmission, lastTransmissionBatchId).ConfigureAwait(false).GetAwaiter().GetResult();
                    this.WaitForPreviousTransmissionsToComplete(transmission).ConfigureAwait(false).GetAwaiter().GetResult();
                    this.SendTransmissionToDisk(transmission);
                }
            }

            return enqueueSucceded;
        }

        /// <summary>
        /// Wait for inflight transmissions to complete. Honors the CancellationToken set by an application on IAsyncFlushable.FlushAsync.
        /// </summary>
        internal async Task<bool> WaitForPreviousTransmissionsToComplete(Transmission transmission)
        {
            var activeTransmissions = this.inFlightTransmissions.Where(p => p.FlushAsyncId == transmission.FlushAsyncId).Select(p => p.TransmissionTask);

            if (activeTransmissions?.Count() > 0)
            {
                // Wait for all transmissions over the wire to complete.
                var inTransitTasks = Task<HttpWebResponseWrapper>.WhenAll(activeTransmissions);
                // Respect passed Cancellation token in FlushAsync call
                var inTransitTasksWithCancellationToken = Task<HttpWebResponseWrapper>.WhenAny(inTransitTasks, new Task<HttpWebResponseWrapper>(() => { return default(HttpWebResponseWrapper); }, transmission.TransmissionCancellationToken));
                await inTransitTasksWithCancellationToken.ConfigureAwait(false);
                return inTransitTasksWithCancellationToken.IsCompleted;
            }

            return true;
        }

        protected void OnTransmissionSent(TransmissionProcessedEventArgs args)
        {
            this.TransmissionSent?.Invoke(this, args);
        }

        private async Task StartSending(Transmission transmission)
        {
            SdkInternalOperationsMonitor.Enter();
            Task<HttpWebResponseWrapper> transmissionTask = null;
            InFlightTransmission inFlightTransmission = default;
            // string lastTransmissionBatchId = null;

            try
            {
                Exception exception = null;
                HttpWebResponseWrapper responseContent = null;

                // Locally self-throttle this payload before we send it
                Transmission acceptedTransmission = this.Throttle(transmission);
                
                // Now that we've self-imposed a throttle, we can try to send the remaining data
                try
                {
                    TelemetryChannelEventSource.Log.TransmissionSendStarted(acceptedTransmission.Id);
                    transmissionTask = acceptedTransmission.SendAsync();
                    // this.inTransmitIds.TryAdd(transmissionTask, transmissionBatchId);

                    inFlightTransmission = new InFlightTransmission(acceptedTransmission.FlushAsyncId, transmissionTask);
                    lock (this.lockObj)
                    {
                        this.inFlightTransmissions.Add(inFlightTransmission);
                    }

                    /*if (acceptedTransmission.HasFlushTask)
                    {
                        lastTransmissionBatchId = transmissionBatchId;
                        lock (this.lockObj)
                        {
                            // Generate new transmissionBatchId for next transmissions to be ready for next IAsyncFlushable.FlushAsync (if called).
                            transmissionBatchId = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
                        }
                    }*/

                    responseContent = await transmissionTask.ConfigureAwait(false); 
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    int currentCapacity = Interlocked.Decrement(ref this.transmissionCount);
                    if (exception != null)
                    {
                        TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(acceptedTransmission.Id, exception.ToLogString());
                    }
                    else
                    {
                        if (responseContent != null)
                        {
                            if (responseContent.StatusCode < 400)
                            {
                                TelemetryChannelEventSource.Log.TransmissionSentSuccessfully(acceptedTransmission.Id,
                                    currentCapacity);
                            }
                            else
                            {
                                TelemetryChannelEventSource.Log.TransmissionSendingFailedWarning(
                                    acceptedTransmission.Id,
                                    responseContent.StatusCode.ToString(CultureInfo.InvariantCulture));
                            }

                            if (TelemetryChannelEventSource.IsVerboseEnabled)
                            {
                                TelemetryChannelEventSource.Log.RawResponseFromAIBackend(acceptedTransmission.Id,
                                    responseContent.Content);
                            }
                        }
                    }

                    if (responseContent == null && exception is HttpRequestException)
                    {
                        // HttpClient.SendAsync throws HttpRequestException on the following scenarios:
                        // "The request failed due to an underlying issue such as network connectivity, DNS failure, server certificate validation or timeout."
                        // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync?view=netstandard-1.6
                        responseContent = new HttpWebResponseWrapper()
                        {
                            // Expectation is that RetryPolicy will attempt retry for this status.
                            StatusCode = ResponseStatusCodes.UnknownNetworkError,
                        };
                    }

                    // this.inTransmitIds.TryRemove(transmissionTask, out string ignoreValue);
                    lock (this.lockObj)
                    {
                        this.inFlightTransmissions.Remove(inFlightTransmission);
                    }

                    if (acceptedTransmission.HasFlushTask)
                    {
                        // Wait for inflight transmissions to complete.
                        // await this.WaitForPreviousTransmissionsToComplete(acceptedTransmission, lastTransmissionBatchId).ConfigureAwait(false);
                        await this.WaitForPreviousTransmissionsToComplete(acceptedTransmission).ConfigureAwait(false);
                    }

                    this.OnTransmissionSent(new TransmissionProcessedEventArgs(acceptedTransmission, exception, responseContent));
                }
            }
            finally
            {
                SdkInternalOperationsMonitor.Exit();
            }
        }

        /*
        /// <summary>
        /// Wait for inflight transmissions to complete. Honors the CancellationToken set by an application on IAsyncFlushable.FlushAsync.
        /// </summary>
        private async Task WaitForPreviousTransmissionsToComplete(Transmission transmission, string lastTransmissionBatchId)
        {
            var activeTransmissions = this.inTransmitIds.Where(p => p.Value == lastTransmissionBatchId).Select(p => p.Key);

            if (activeTransmissions != null)
            {
                // Wait for all transmissions over the wire to complete.
                var inTransitTasks = Task<HttpWebResponseWrapper>.WhenAll(activeTransmissions);
                // Respect passed Cancellation token in FlushAsync call
                await Task<HttpWebResponseWrapper>.WhenAny(inTransitTasks, new Task<HttpWebResponseWrapper>(() => { return default(HttpWebResponseWrapper); }, transmission.TransmissionCancellationToken)).ConfigureAwait(false);
                // Remove processed transmission items from list to free the memory
                activeTransmissions.ToList().ForEach(key => this.inTransmitIds.TryRemove(key, out string ignoreValue));
            }
        }*/

        /// <summary>
        /// Checks if the transmission throttling policy allows for sending another request.
        /// If so, this method will add a request to the current throttle count (unless peeking).
        /// </summary>
        /// <returns>The number of events that are able to be sent.</returns>
        private int IsTransmissionSendable(int numEvents, bool peek = false)
        {
            if (!this.applyThrottle)
            {
                return numEvents;
            }

            long throttleWindowId = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / this.ThrottleWindow);
            bool isInThrottleWindow = throttleWindowId == this.currentThrottleWindowId;
            if (isInThrottleWindow && this.currentItemsCount < this.ThrottleLimit)
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
            Tuple<Transmission, Transmission> transmissions = transmission.Split((transmissionLength) => 
            {
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
                // On transmission split, TaskCompletionSource is lost. Copy TaskCompletionSource to acceptedTransmission.
                acceptedTransmission.SetFlushTaskCompletionSource(transmission.GetFlushTaskCompletionSource());
                // rejectedTransmission calls policy, which inturn moves transmission to storage. 
                this.SendTransmissionThrottleRejection(rejectedTransmission, transmission.HasFlushTask);
            }
            
            return acceptedTransmission;
        }

        private void SendTransmissionThrottleRejection(Transmission rejectedTransmission, bool hasFlushTask)
        {
            var statusDescription = hasFlushTask ? "SendToDisk" : "Internally Throttled";
            WebException exception = new WebException(
                "Transmission was split by local throttling policy",
                null,
                System.Net.WebExceptionStatus.Success,
                null);
            this.OnTransmissionSent(new TransmissionProcessedEventArgs(
                rejectedTransmission, 
                exception, 
                new HttpWebResponseWrapper()
                {
                    StatusCode = ResponseStatusCodes.ResponseCodeTooManyRequests,
                    StatusDescription = statusDescription,
                    RetryAfterHeader = null,
                }));
        }

        /// <summary>
        /// Called when Sender is out of capacity, AsyncFlushTransmissionPolicy gets invoked and items will be moved to storage.
        /// </summary>
        private void SendTransmissionToDisk(Transmission transmission)
        {
            this.OnTransmissionSent(new TransmissionProcessedEventArgs(
                transmission,
                null,
                new HttpWebResponseWrapper()
                {
                    StatusCode = ResponseStatusCodes.Success,
                    StatusDescription = "SendToDisk",
                    RetryAfterHeader = null,
                }));
        }
    }
}
