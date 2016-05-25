namespace Microsoft.ApplicationInsights.Channel.Implementation
{
    using System;
    using System.Collections.Specialized;
    using System.Web.Script.Serialization;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    internal static class TransmissionPolicyHelpers
    {
        public const int SlotDelayInSeconds = 10;
        private const int MaxDelayInSeconds = 3600;
        private static readonly Random Random = new Random();
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static BackendResponse GetBackendResponse(TransmissionProcessedEventArgs args)
        {
            BackendResponse backendResponse = null;

            if (args != null && args.Response != null)
            {
                string responseContent = args.Response.Content;
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
            }

            return backendResponse;
        }

        // Calculates the time to wait before retrying in case of an error based on
        // http://en.wikipedia.org/wiki/Exponential_backoff
        public static TimeSpan GetBackOffTime(int consecutiveErrors, string headerValue)
        {
            TimeSpan retryAfterTimeSpan;
            if (!TryParseRetryAfter(headerValue, out retryAfterTimeSpan))
            {
                double delayInSeconds;

                if (consecutiveErrors <= 1)
                {
                    delayInSeconds = SlotDelayInSeconds;
                }
                else
                {
                    double backOffSlot = (Math.Pow(2, consecutiveErrors) - 1) / 2;
                    var backOffDelay = Random.Next(1, (int)Math.Min(backOffSlot * SlotDelayInSeconds, int.MaxValue));
                    delayInSeconds = Math.Max(Math.Min(backOffDelay, MaxDelayInSeconds), SlotDelayInSeconds);
                }

                TelemetryChannelEventSource.Log.BackoffTimeSetInSeconds(delayInSeconds);
                retryAfterTimeSpan = TimeSpan.FromSeconds(delayInSeconds);
            }

            TelemetryChannelEventSource.Log.BackoffInterval(retryAfterTimeSpan.TotalSeconds);
            return retryAfterTimeSpan;
        }

        public static TimeSpan GetBackOffTime(int consecutiveErrors, NameValueCollection headers)
        {
            string retryAfterHeader = string.Empty;
            if (headers != null)
            {
                retryAfterHeader = headers.Get("Retry-After");
            }

            return GetBackOffTime(consecutiveErrors, retryAfterHeader);
        }

        private static bool TryParseRetryAfter(string retryAfter, out TimeSpan retryAfterTimeSpan)
        {
            retryAfterTimeSpan = TimeSpan.FromSeconds(0);

            if (string.IsNullOrEmpty(retryAfter))
            {
                return false;
            }

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
    }
}
