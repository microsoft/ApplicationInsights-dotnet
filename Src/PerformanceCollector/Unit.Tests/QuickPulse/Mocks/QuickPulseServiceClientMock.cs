namespace Unit.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseServiceClientMock : IQuickPulseServiceClient
    {
        private readonly object lockObject = new object();

        public int PingCount { get; private set; }

        public List<QuickPulseDataSample> Samples { get; } = new List<QuickPulseDataSample>();

        public bool? ReturnValueFromPing { private get; set; }

        public bool? ReturnValueFromSubmitSample { private get; set; }

        public int? LastSampleBatchSize { get; private set; }

        private List<int> batches = new List<int>();

        public void Reset()
        {
            lock (this.lockObject)
            {
                this.PingCount = 0;
                this.LastSampleBatchSize = null;

                this.Samples.Clear();
            }
        }

        public bool? Ping(string instrumentationKey)
        {
            lock (this.lockObject)
            {
                this.PingCount++;
            }

            return this.ReturnValueFromPing;
        }

        public bool? SubmitSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey)
        {
            lock (this.lockObject)
            {
                this.batches.Add(samples.Count());
                this.LastSampleBatchSize = samples.Count();
                this.Samples.AddRange(samples);
            }

            return this.ReturnValueFromSubmitSample;
        }
    }
}