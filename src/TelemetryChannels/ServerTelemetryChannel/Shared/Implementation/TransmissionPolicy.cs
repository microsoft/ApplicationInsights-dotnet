namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Specialized;
    
    internal abstract class TransmissionPolicy
    {
        protected const int SlotDelayInSeconds = 10;
        private const int MaxDelayInSeconds = 3600;

        private readonly string policyName;
        private readonly Random random = new Random();

        protected TransmissionPolicy()
        {
            this.policyName = this.GetType().ToString();
        }

        public int ConsecutiveErrors { get; set; }

        public int? MaxSenderCapacity { get; protected set; }

        public int? MaxBufferCapacity { get; protected set; }

        public int? MaxStorageCapacity { get; protected set; }

        protected Transmitter Transmitter { get; private set; }

        public void Apply()
        {
            if (this.Transmitter == null)
            {
                throw new InvalidOperationException("Transmission policy has not been initialized.");
            }

            try
            {
                this.Transmitter.ApplyPolicies();
            }
            catch (Exception exp)
            {
                TelemetryChannelEventSource.Log.ApplyPoliciesError(exp.ToString());
            }
        }

        public virtual void Initialize(Transmitter transmitter)
        {
            if (transmitter == null)
            {
                throw new ArgumentNullException("transmitter");
            }

            this.Transmitter = transmitter;
        }

        // Calculates the time to wait before retrying in case of an error based on
        // http://en.wikipedia.org/wiki/Exponential_backoff
        public virtual TimeSpan GetBackOffTime(string headerValue)
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
                    var backOffDelay = this.random.Next(1, (int)Math.Min(backOffSlot * SlotDelayInSeconds, int.MaxValue));
                    delayInSeconds = Math.Max(Math.Min(backOffDelay, MaxDelayInSeconds), SlotDelayInSeconds);
                }

                TelemetryChannelEventSource.Log.BackoffTimeSetInSeconds(delayInSeconds);
                retryAfterTimeSpan = TimeSpan.FromSeconds(delayInSeconds);
            }

            TelemetryChannelEventSource.Log.BackoffInterval(retryAfterTimeSpan.TotalSeconds);
            return retryAfterTimeSpan;
        }

        public virtual TimeSpan GetBackOffTime(NameValueCollection headers)
        {
            string retryAfterHeader = string.Empty;
            if (headers != null)
            {
                retryAfterHeader = headers.Get("Retry-After");
            }

            return this.GetBackOffTime(retryAfterHeader);
        }

        protected void LogCapacityChanged()
        {
            if (this.MaxSenderCapacity.HasValue)
            {
                TelemetryChannelEventSource.Log.SenderCapacityChanged(this.policyName, this.MaxSenderCapacity.Value);
            }
            else
            {
                TelemetryChannelEventSource.Log.SenderCapacityReset(this.policyName);
            }

            if (this.MaxBufferCapacity.HasValue)
            {
                TelemetryChannelEventSource.Log.BufferCapacityChanged(this.policyName, this.MaxBufferCapacity.Value);
            }
            else
            {
                TelemetryChannelEventSource.Log.BufferCapacityReset(this.policyName);
            }

            if (this.MaxStorageCapacity.HasValue)
            {
                TelemetryChannelEventSource.Log.StorageCapacityChanged(this.policyName, this.MaxStorageCapacity.Value);
            }
            else
            {
                TelemetryChannelEventSource.Log.StorageCapacityReset(this.policyName);
            }
        }

        private bool TryParseRetryAfter(string retryAfter, out TimeSpan retryAfterTimeSpan)
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
