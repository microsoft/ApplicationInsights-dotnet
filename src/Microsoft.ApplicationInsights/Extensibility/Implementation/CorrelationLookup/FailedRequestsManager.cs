namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using Extensibility.Implementation.Tracing;

    internal class FailedRequestsManager
    {
        private ConcurrentDictionary<string, FailedResult> failingInstrumentationKeys = new ConcurrentDictionary<string, FailedResult>();

        /// <summary>
        /// FetchAppIdFromService failed.
        /// Registers failure for further action in future.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for which the failure occurred.</param>
        /// <param name="httpStatusCode">Response code from AppId Endpoint.</param>
        public void RegisterFetchFailure(string instrumentationKey, HttpStatusCode httpStatusCode)
        {
            this.failingInstrumentationKeys[instrumentationKey] = new FailedResult(httpStatusCode);

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
                this.failingInstrumentationKeys[instrumentationKey] = new FailedResult(httpWebResponse.StatusCode);
            }
            else
            {
                this.failingInstrumentationKeys[instrumentationKey] = new FailedResult();
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
            // We have arbitrarily chosen 5 second delay between trying to get app Id once we get a failure while trying to get it. 
            // This is to throttle tries between failures to safeguard against performance hits. The impact would be that telemetry generated during this interval would not have x-component correlation id.
            private readonly TimeSpan intervalBetweenRetries = TimeSpan.FromSeconds(30);

            /// <summary>
            /// Initializes a new instance of the <see cref="FailedResult" /> class.
            /// </summary>
            /// <param name="httpStatusCode">Failure response code. Used to determine if we should retry requests.</param>
            public FailedResult(HttpStatusCode httpStatusCode = HttpStatusCode.OK)
            {
                this.RetryAfterTime = DateTime.UtcNow + this.intervalBetweenRetries;

                // Do not retry 4XX failures.
                var failureCode = (int)httpStatusCode;
                this.ShouldRetry = !(failureCode >= 400 && failureCode < 500);
            }

            private DateTime RetryAfterTime { get; set; }

            private bool ShouldRetry { get; set; }

            public bool CanRetry()
            {
                return this.ShouldRetry && DateTime.UtcNow > this.RetryAfterTime;
            }
        }
    }
}
