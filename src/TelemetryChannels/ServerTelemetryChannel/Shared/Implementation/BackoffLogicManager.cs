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

        private TaskTimer pauseTimer = new TaskTimer { Delay = TimeSpan.FromSeconds(SlotDelayInSeconds) };
        private bool exponentialBackoffReported = false;
        
        public BackoffLogicManager(TimeSpan defaultBackoffEnabledReportingInterval)
        {
            this.defaultBackoffEnabledReportingInterval = defaultBackoffEnabledReportingInterval;
        }

        /// <summary>
        /// Gets or sets the number of consecutive errors SDK transmitter got so far while sending telemetry to backend.
        /// </summary>
        public int ConsecutiveErrors { get; set; }

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
            if (!this.exponentialBackoffReported && this.pauseTimer.Delay > this.defaultBackoffEnabledReportingInterval)
            {
                TelemetryChannelEventSource.Log.BackoffEnabled(this.pauseTimer.Delay.TotalMinutes, statusCode);
                this.exponentialBackoffReported = true;
            }
        }

        public void ReportBackoffDisabled()
        {
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
