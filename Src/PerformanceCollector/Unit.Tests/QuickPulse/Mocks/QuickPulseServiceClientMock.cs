namespace Unit.Tests
{
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseServiceClientMock : IQuickPulseServiceClient
    {
        public int PingCount { get; private set; }

        public List<QuickPulseDataSample> Samples { get; } = new List<QuickPulseDataSample>();

        public int SampleCount => this.Samples.Count;

        public bool ReturnValueFromPing { get; set; }

        public bool ReturnValueFromSubmitSample { get; set; }

        public void Reset()
        {
            this.PingCount = 0;
            
            this.Samples.Clear();
        }

        public bool Ping()
        {
            this.PingCount++;

            return this.ReturnValueFromPing;
        }

        public bool SubmitSample(QuickPulseDataSample sample)
        {
            this.Samples.Add(sample);

            return this.ReturnValueFromSubmitSample;
        }
    }
}