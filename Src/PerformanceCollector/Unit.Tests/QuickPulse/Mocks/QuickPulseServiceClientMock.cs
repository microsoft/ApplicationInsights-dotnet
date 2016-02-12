namespace Unit.Tests
{
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseServiceClientMock : IQuickPulseServiceClient
    {
        private readonly object lockObject = new object();

        public int PingCount { get; private set; }

        public List<QuickPulseDataSample> Samples { get; } = new List<QuickPulseDataSample>();

        public bool ReturnValueFromPing { private get; set; }

        public bool ReturnValueFromSubmitSample { private get; set; }

        public void Reset()
        {
            lock (this.lockObject)
            {
                this.PingCount = 0;

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
                this.Samples.AddRange(samples);
            }

            return this.ReturnValueFromSubmitSample;
        }
    }
}