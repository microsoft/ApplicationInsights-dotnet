namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseServiceClientMock : IQuickPulseServiceClient
    {
        private readonly object lockObject = new object();

        private List<QuickPulseDataSample> samples = new List<QuickPulseDataSample>();

        public int PingCount { get; private set; }

        public bool? ReturnValueFromPing { private get; set; }

        public bool? ReturnValueFromSubmitSample { private get; set; }

        public int? LastSampleBatchSize { get; private set; }

        private List<int> batches = new List<int>();

        public DateTimeOffset? LastPingTimestamp { get; private set; }

        public string LastPingInstance { get; private set; }

        public bool AlwaysThrow { get; set; } = false;

        public List<QuickPulseDataSample> SnappedSamples
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.samples.ToList();
                }
            }
        }

        public Uri ServiceUri { get; }

        public void Reset()
        {
            lock (this.lockObject)
            {
                this.PingCount = 0;
                this.LastSampleBatchSize = null;
                this.LastPingTimestamp = null;
                this.LastPingInstance = string.Empty;

                this.samples.Clear();
            }
        }

        public bool? Ping(string instrumentationKey, DateTimeOffset timestamp)
        {
            lock (this.lockObject)
            {
                this.PingCount++;
                this.LastPingTimestamp = timestamp;
            }

            if (this.AlwaysThrow)
            {
                throw new InvalidOperationException("Mock is set to always throw");
            }

            return this.ReturnValueFromPing;
        }

        public bool? SubmitSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey)
        {
            lock (this.lockObject)
            {
                this.batches.Add(samples.Count());
                this.LastSampleBatchSize = samples.Count();
                this.samples.AddRange(samples);
            }

            if (this.AlwaysThrow)
            {
                throw new InvalidOperationException("Mock is set to always throw");
            }

            return this.ReturnValueFromSubmitSample;
        }
    }
}