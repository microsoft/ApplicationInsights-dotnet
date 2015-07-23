// <copyright file="Sender.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Fetch transmissions from the storage and sends it. 
    /// </summary>
    internal class Sender : IDisposable
    {
        /// <summary>
        /// A wait handle that flags the sender when to start sending again. The type is protected for unit test.
        /// </summary>
        protected readonly AutoResetEvent DelayHandler;

        /// <summary>
        /// When storage is empty it will be queried again after this interval. 
        /// </summary>
        private readonly TimeSpan sendingIntervalOnNoData = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Holds the maximum time for the exponential back-off algorithm. The sending interval will grow on every HTTP Exception until this max value.
        /// </summary>
        private readonly TimeSpan maxIntervalBetweenRetries = TimeSpan.FromHours(1);

        /// <summary>
        /// A wait handle that is being set when Sender is no longer sending.
        /// </summary>
        private readonly AutoResetEvent stoppedHandler;

        /// <summary>
        /// The default sending interval.
        /// </summary>
        private readonly TimeSpan defaultSendingInterval;

        /// <summary>
        /// A boolean value that indicates if the sender should be stopped. The sender's while loop is checking this boolean value.  
        /// </summary>
        private bool stopped;

        /// <summary>
        /// The amount of time to wait, in the stop method, until the last transmission is sent. 
        /// If time expires, the stop method will return even if the transmission hasn't been sent. 
        /// </summary>
        private TimeSpan drainingTimeout;

        /// <summary>
        /// The transmissions storage.
        /// </summary>
        private StorageBase storage;

        /// <summary>
        /// The number of times this object was disposed.
        /// </summary>
        private int disposeCount = 0;

        /// <summary>
        /// Holds the transmitter.
        /// </summary>
        private PersistenceTransmitter transmitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sender"/> class.
        /// </summary>
        /// <param name="storage">The storage that holds the transmissions to send.</param>
        /// <param name="transmitter">
        /// The persistence transmitter that manages this Sender. 
        /// The transmitter will be used as a configuration class, it exposes properties like SendingInterval that will be read by Sender.
        /// </param>
        /// <param name="startSending">A boolean value that determines if Sender should start sending immediately. This is only used for unit tests.</param>
        internal Sender(StorageBase storage, PersistenceTransmitter transmitter, bool startSending = true)
        {
            this.stopped = false;
            this.DelayHandler = new AutoResetEvent(false);
            this.stoppedHandler = new AutoResetEvent(false);
            this.drainingTimeout = TimeSpan.FromSeconds(100);
            this.defaultSendingInterval = TimeSpan.FromSeconds(5);

            // TODO: instead of a circualr reference, pass the TelemetryConfiguration.
            this.transmitter = transmitter;
            this.storage = storage;

            if (startSending)
            {
                Task.Factory.StartNew(this.SendLoop, TaskCreationOptions.LongRunning)
                    .ContinueWith(t => CoreEventSource.Log.LogVerbose("Sender: Failure in SendLoop: Exception: " + t.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// Gets the interval between each successful sending.
        /// </summary>
        private TimeSpan SendingInterval
        {
            get
            {
                if (this.transmitter.SendingInterval != null)
                {
                    return this.transmitter.SendingInterval.Value;
                }

                return this.defaultSendingInterval;
            }
        }

        /// <summary>
        /// Disposes the managed objects.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref this.disposeCount) == 1)
            {
                this.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();

#if NET35
                this.DelayHandler.Close();
#else
                this.DelayHandler.Dispose();
#endif

#if NET35
               this.stoppedHandler.Close();
#else
                this.stoppedHandler.Dispose();
#endif
            }
        }

        /// <summary>
        /// Stops the sender. 
        /// </summary>
        internal Task StopAsync()
        {   
            // After delayHandler is set, a sending iteration will immidiately start. 
            // Seting <c>stopped</c> to ture, will cause the iteration to skip the actual sending and stop immediately. 
            this.stopped = true;
            this.DelayHandler.Set();

            // if delayHandler was set while a transmision was being sent, the return task waill wait for it to finsih, for an additional second,
            // before it will mark the task as completed. 
            return Task.Factory.StartNew(() =>
                {
                    try
                    {   
                        this.stoppedHandler.WaitOne(this.drainingTimeout);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                });
        }

        /// <summary>
        /// Send transmissions in a loop. 
        /// </summary>
        protected void SendLoop()
        {
            TimeSpan prevSendingInterval = TimeSpan.Zero;
            TimeSpan sendingInterval = this.sendingIntervalOnNoData;
            try
            {
                while (!this.stopped)
                {
                    using (StorageTransmission transmission = this.storage.Peek())
                    {
                        if (this.stopped)
                        {
                            // This second verfiication is required for cases where 'stopped' was set while peek was happening. 
                            // Once the actual sending starts the design is to wait until it finishes and deletes the transmission. 
                            // So no extra validation is required.
                            break;
                        }

                        // If there is a transmission to send - send it. 
                        if (transmission != null)
                        {
                            bool shouldRetry = this.Send(transmission, ref sendingInterval);
                            if (!shouldRetry)
                            {
                                // If retry is not required - delete the transmission.
                                this.storage.Delete(transmission);
                            }
                        }
                        else
                        {
                            sendingInterval = this.sendingIntervalOnNoData;
                        }
                    }

                    LogInterval(prevSendingInterval, sendingInterval);
                    this.DelayHandler.WaitOne(sendingInterval);
                    prevSendingInterval = sendingInterval;
                }

                this.stoppedHandler.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Sends a transmission and handle errors.
        /// </summary>
        /// <param name="transmission">The transmission to send.</param>
        /// <param name="nextSendInterval">When this value returns it will hold a recommendation for when to start the next sending iteration.</param>
        /// <returns>A boolean value that indicates if there was a retriable error.</returns>        
        protected virtual bool Send(StorageTransmission transmission, ref TimeSpan nextSendInterval)
        {   
            try
            {
                if (transmission != null)
                {
                    transmission.SendAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    
                    // After a successful sending, try immeidiately to send another transmission. 
                    nextSendInterval = this.SendingInterval;
                }
            }
            catch (WebException e)
            {
                int? statusCode = GetStatusCode(e);
                nextSendInterval = this.CalculateNextInterval(statusCode, nextSendInterval, this.maxIntervalBetweenRetries);
                return IsRetryable(statusCode, e.Status);
            }
            catch (Exception e)
            {
                nextSendInterval = this.CalculateNextInterval(null, nextSendInterval, this.maxIntervalBetweenRetries);
                string msg = string.Format(CultureInfo.InvariantCulture, "Unknown exception during sending: {0}", e);
                CoreEventSource.Log.LogVerbose(msg);
            }

            return false;
        }

        /// <summary>
        /// Log next interval. Only log the interval when it changes by more then a minute. So if interval grow by 1 minute or decreased by 1 minute it will be logged. 
        /// Logging every interval will just make the log noisy. 
        /// </summary>        
        private static void LogInterval(TimeSpan prevSendInterval, TimeSpan nextSendInterval)
        {   
            if (Math.Abs(nextSendInterval.TotalSeconds - prevSendInterval.TotalSeconds) > 60)
            {
                CoreEventSource.Log.LogVerbose("next sending interval: " + nextSendInterval);
            }
        }

        /// <summary>
        /// Return the status code from the web exception or null if no such code exists. 
        /// </summary>
        private static int? GetStatusCode(WebException e)
        {   
            HttpWebResponse httpWebResponse = e.Response as HttpWebResponse;
            if (httpWebResponse != null)
            {
                return (int)httpWebResponse.StatusCode;
            }

            return null;
        }

        /// <summary>
        /// Returns true if <paramref name="httpStatusCode" /> or <paramref name="webExceptionStatus" /> are retriable.
        /// </summary>
        private static bool IsRetryable(int? httpStatusCode, WebExceptionStatus webExceptionStatus)
        {
#if NET40 || NET45 // WINRT doesn't support ProxyNameResolutionFailure/NameResolutionFailure/Timeout/ConnectFailure, for WinPhone this seems like a corner scenario and we don't want to spend the effot to test it now.
            switch (webExceptionStatus)
            {
                case WebExceptionStatus.ProxyNameResolutionFailure:
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.ConnectFailure:
                    return true;
            }
#endif 
            if (httpStatusCode == null)
            {
                return false;
            }

            switch (httpStatusCode.Value)
            {
                case 503: // Server in maintance. 
                case 408: // invalid request
                case 500: // Internal Server Error                                                
                case 502: // Bad Gateway, can be common when there is no network. 
                case 511: // Network Authentication Required
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the next interval using exponential back-off algorithm (with the exceptions of few error codes that reset the interval to <see cref="SendingInterval"/>.
        /// </summary>        
        private TimeSpan CalculateNextInterval(int? httpStatusCode, TimeSpan currentSendInterval, TimeSpan maxInterval)
        {
            // if item is expired, no need for exponential back-off
            if (httpStatusCode != null && httpStatusCode.Value == 400 /* expired */)
            {
                return this.SendingInterval;
            }

            // exponential back-off.
            if (currentSendInterval.TotalSeconds == 0)
            {
                return TimeSpan.FromSeconds(1);
            }
            else
            {
                double nextIntervalInSeconds = Math.Min(currentSendInterval.TotalSeconds * 2, maxInterval.TotalSeconds);

                return TimeSpan.FromSeconds(nextIntervalInSeconds);
            }
        }
    }
}