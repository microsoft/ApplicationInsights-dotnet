namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net;
    using Extensibility.Implementation.Tracing;

    internal class FailedRequestsManager
    {
        // Delay between trying to get app Id once we get a failure while trying to get it. 
        // This is to throttle tries between failures to safeguard against performance hits. The impact would be that telemetry generated during this interval would not have x-component correlation id.
        private readonly TimeSpan retryWaitTimeSeconds;

        private ConcurrentDictionary<string, FailedResult> failingInstrumentationKeys = new ConcurrentDictionary<string, FailedResult>();

        internal FailedRequestsManager(int retryWaitTimeSeconds = 30)
        {
            this.retryWaitTimeSeconds = TimeSpan.FromSeconds(retryWaitTimeSeconds);
        }

        /// <summary>
        /// FetchAppIdFromService failed.
        /// Registers failure for further action in future.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which the failure occurred.</param>
        /// <param name="httpStatusCode">Response code from AppId Endpoint.</param>
        public void RegisterFetchFailure(string instrumentationKey, HttpStatusCode httpStatusCode)
        {
            Debug.WriteLine("TryAdd failed request: " + this.failingInstrumentationKeys.TryAdd(instrumentationKey, new FailedResult(this.retryWaitTimeSeconds, httpStatusCode)));

            CorrelationLookupEventSource.Log.FetchAppIdFailedWithResponseCode(httpStatusCode.ToString());
        }

        /// <summary>
        /// Registers failure for further action in future.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which the failure occurred.</param>
        /// <param name="ex">Exception indicating failure.</param>
        public void RegisterFetchFailure(string instrumentationKey, Exception ex)
        {
            if (ex is AggregateException ae)
            {
                var innerException = ae.Flatten().InnerException;
                if (innerException != null)
                {
                    this.RegisterFetchFailure(instrumentationKey, innerException);
                    return;
                }
            }
            else if (ex is WebException webException && webException.Response != null && webException.Response is HttpWebResponse httpWebResponse)
            {
                Debug.WriteLine("TryAdd failed request: " + this.failingInstrumentationKeys.TryAdd(instrumentationKey, new FailedResult(this.retryWaitTimeSeconds, httpWebResponse.StatusCode)));
            }
            else
            {
                Debug.WriteLine("TryAdd failed request: " + this.failingInstrumentationKeys.TryAdd(instrumentationKey, new FailedResult(this.retryWaitTimeSeconds)));
            }

            CorrelationLookupEventSource.Log.FetchAppIdFailed(this.GetExceptionDetailString(ex));
        }

        public bool CanRetry(string instrumentationKey)
        {
            if (this.failingInstrumentationKeys.TryGetValue(instrumentationKey, out FailedResult lastFailedResult))
            {
                if (lastFailedResult.CanRetry())
                {
                    this.failingInstrumentationKeys.TryRemove(instrumentationKey, out FailedResult value);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            Debug.WriteLine("no failed result found, is safe to retry.");
            return true;
        }

        private string GetExceptionDetailString(Exception ex)
        {
            if (ex is AggregateException ae)
            {
                return ae.Flatten().InnerException.ToInvariantString();
            }

            return ex.ToInvariantString();
        }

        /// <summary>
        /// Structure that represents a failed fetch app Id call.
        /// </summary>
        private class FailedResult
        {
            private readonly DateTime retryAfterTime;
            private readonly bool shouldRetry;

            /// <summary>
            /// Initializes a new instance of the <see cref="FailedResult" /> class.
            /// </summary>
            /// <param name="retryAfter">time to wait before a retry</param>
            /// <param name="httpStatusCode">Failure response code. Used to determine if we should retry requests.</param>
            public FailedResult(TimeSpan retryAfter, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
            {
                this.retryAfterTime = DateTime.UtcNow + retryAfter;
                Debug.WriteLine($"request failed, wait until: {this.retryAfterTime.ToString("HH:mm:ss:fffff")}");

                // Do not retry 4XX failures.
                var failureCode = (int)httpStatusCode;
                var is4XX = failureCode >= 400 && failureCode < 500;
                this.shouldRetry = !is4XX;
                Debug.WriteLine($"failureCode: {failureCode} is4XX: {is4XX} shouldRetry: {this.shouldRetry}");
            }

            public bool CanRetry()
            {
                var value = this.shouldRetry && DateTime.UtcNow > this.retryAfterTime;
                Debug.WriteLine($"CanRetry: {value}");
                return value;
            }
        }
    }
}
