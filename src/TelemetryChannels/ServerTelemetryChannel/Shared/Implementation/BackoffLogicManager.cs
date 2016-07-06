namespace Microsoft.ApplicationInsights.Channel.Implementation
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

#if CORE_PCL || NET45 || NET46
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class BackoffLogicManager : IDisposable
    {
        private const int SlotDelayInSeconds = 10;
        private const int MaxDelayInSeconds = 3600;

        private static readonly Random Random = new Random();
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        private readonly TimeSpan defaultBackoffEnabledReportingInterval;
        private readonly object lockConsecutiveErrors = new object();
        private readonly TimeSpan minIntervalToUpdateConsecutiveErrors;

        private TaskTimer pauseTimer = new TaskTimer { Delay = TimeSpan.FromSeconds(SlotDelayInSeconds) };
        private bool exponentialBackoffReported = false;
        private int consecutiveErrors;
        private DateTimeOffset nextMinTimeToUpdateConsecutiveErrors = DateTimeOffset.MinValue;

        public BackoffLogicManager(TimeSpan defaultBackoffEnabledReportingInterval)
        {
            this.defaultBackoffEnabledReportingInterval = defaultBackoffEnabledReportingInterval;
            this.minIntervalToUpdateConsecutiveErrors = TimeSpan.FromSeconds(SlotDelayInSeconds);
        }

        internal BackoffLogicManager(TimeSpan defaultBackoffEnabledReportingInterval, TimeSpan minIntervalToUpdateConsecutiveErrors) 
            : this(defaultBackoffEnabledReportingInterval)
        {
            // This constructor is used from unit tests
            this.minIntervalToUpdateConsecutiveErrors = minIntervalToUpdateConsecutiveErrors;
        }

        /// <summary>
        /// Gets or sets the number of consecutive errors SDK transmitter got so far while sending telemetry to backend.
        /// </summary>
        public int ConsecutiveErrors
        {
            get
            {
                return this.consecutiveErrors;
            }

            set
            {
                lock (this.lockConsecutiveErrors)
                {
                    if (value == 0)
                    {
                        this.consecutiveErrors = 0;
                        return;
                    }

                    // Do not increase number of errors more often than minimum interval (SlotDelayInSeconds) 
                    // since we have 3 senders and all of them most likely would fail if we have intermittent error  
                    if (DateTimeOffset.UtcNow > this.nextMinTimeToUpdateConsecutiveErrors)
                    {
                        this.consecutiveErrors = value;
                        this.nextMinTimeToUpdateConsecutiveErrors = DateTimeOffset.UtcNow + this.minIntervalToUpdateConsecutiveErrors;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the last status code SDK received from the backend.
        /// </summary>
        public int LastStatusCode { get; set; }

        internal TimeSpan CurrentDelay
        {
            get { return this.pauseTimer.Delay; }
        }

        public void ScheduleRestore(WebHeaderCollection headers, Func<Task> elapsedFunc)
        {
            // Back-off for the Delay duration and enable sending capacity
            string retryAfterHeader = string.Empty;
            if (headers != null)
            {
                retryAfterHeader = headers.Get("Retry-After");
            }

            this.ScheduleRestore(retryAfterHeader, elapsedFunc);
        }

        public void ScheduleRestore(string retryAfterHeader, Func<Task> elapsedFunc)
        {
            // Back-off for the Delay duration and enable sending capacity
            this.pauseTimer.Delay = this.GetBackOffTime(retryAfterHeader);
            this.pauseTimer.Start(elapsedFunc);
        }

        public BackendResponse GetBackendResponse(string responseContent)
        {
            BackendResponse backendResponse = null;

            try
            {
                if (!string.IsNullOrEmpty(responseContent))
                {
                    backendResponse = Serializer.Deserialize<BackendResponse>(responseContent);
                }
            }
            catch (ArgumentException exp)
            {
                TelemetryChannelEventSource.Log.BreezeResponseWasNotParsedWarning(exp.Message, responseContent);
                backendResponse = null;
            }
            catch (InvalidOperationException exp)
            {
                TelemetryChannelEventSource.Log.BreezeResponseWasNotParsedWarning(exp.Message, responseContent);
                backendResponse = null;
            }

            return backendResponse;
        }

        public void ReportBackoffEnabled(int statusCode)
        {
            this.LastStatusCode = statusCode;

            if (!this.exponentialBackoffReported && this.pauseTimer.Delay > this.defaultBackoffEnabledReportingInterval)
            {
                TelemetryChannelEventSource.Log.BackoffEnabled(this.pauseTimer.Delay.TotalMinutes, statusCode);
                this.exponentialBackoffReported = true;
            }
        }

        public void ReportBackoffDisabled()
        {
            this.LastStatusCode = 200;

            if (this.exponentialBackoffReported)
            {
                TelemetryChannelEventSource.Log.BackoffDisabled();
                this.exponentialBackoffReported = false;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Calculates the time to wait before retrying in case of an error based on
        // http://en.wikipedia.org/wiki/Exponential_backoff
        protected virtual TimeSpan GetBackOffTime(string headerValue)
        {
            TimeSpan retryAfterTimeSpan;
            if (!this.TryParseRetryAfter(headerValue, out retryAfterTimeSpan))
            {
                double delayInSeconds;

                if (this.ConsecutiveErrors <= 1)
                {
                    delayInSeconds = SlotDelayInSeconds;
                }
                else
                {
                    double backOffSlot = (Math.Pow(2, this.ConsecutiveErrors) - 1) / 2;
                    var backOffDelay = Random.Next(1, (int)Math.Min(backOffSlot * SlotDelayInSeconds, int.MaxValue));
                    delayInSeconds = Math.Max(Math.Min(backOffDelay, MaxDelayInSeconds), SlotDelayInSeconds);
                }

                TelemetryChannelEventSource.Log.BackoffTimeSetInSeconds(delayInSeconds);
                retryAfterTimeSpan = TimeSpan.FromSeconds(delayInSeconds);
            }

            TelemetryChannelEventSource.Log.BackoffInterval(retryAfterTimeSpan.TotalSeconds);
            return retryAfterTimeSpan;
        }

        private bool TryParseRetryAfter(string retryAfter, out TimeSpan retryAfterTimeSpan)
        {
            retryAfterTimeSpan = TimeSpan.FromSeconds(0);

            if (string.IsNullOrEmpty(retryAfter))
            {
                return false;
            }

            TelemetryChannelEventSource.Log.RetryAfterHeaderIsPresent(retryAfter);

            var now = DateTimeOffset.UtcNow;
            DateTimeOffset retryAfterDate;
            if (DateTimeOffset.TryParse(retryAfter, out retryAfterDate))
            {
                if (retryAfterDate > now)
                {
                    retryAfterTimeSpan = retryAfterDate - now;
                    return true;
                }

                return false;
            }

            TelemetryChannelEventSource.Log.TransmissionPolicyRetryAfterParseFailedWarning(retryAfter);

            return false;
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
