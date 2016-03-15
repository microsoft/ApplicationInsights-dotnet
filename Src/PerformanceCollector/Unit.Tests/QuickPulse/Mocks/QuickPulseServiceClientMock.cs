namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseServiceClientMock : IQuickPulseServiceClient
    {
        private List<QuickPulseDataSample> samples = new List<QuickPulseDataSample>();

        private readonly object countersLock = new object();

        public volatile bool CountersEnabled = true;

        public readonly object ResponseLock = new object();

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
                lock (this.countersLock)
                {
                    return this.samples.ToList();
                }
            }
        }

        public Uri ServiceUri { get; }

        public void Reset()
        {
            lock (this.countersLock)
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
            lock (this.ResponseLock)
            {
                if (this.CountersEnabled)
                {
                    lock (this.countersLock)
                    {
                        this.PingCount++;
                        this.LastPingTimestamp = timestamp;
                    }
                }

                if (this.AlwaysThrow)
                {
                    throw new InvalidOperationException("Mock is set to always throw");
                }

                return this.ReturnValueFromPing;
            }
        }

        public bool? SubmitSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey)
        {
            lock (this.ResponseLock)
            {
                if (this.CountersEnabled)
                {
                    lock (this.countersLock)
                    {
                        this.batches.Add(samples.Count());
                        this.LastSampleBatchSize = samples.Count();
                        this.samples.AddRange(samples);
                    }
                }

                if (this.AlwaysThrow)
                {
                    throw new InvalidOperationException("Mock is set to always throw");
                }

                return this.ReturnValueFromSubmitSample;
            }
        }
    }
}