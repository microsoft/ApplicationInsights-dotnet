namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseServiceClientMock : IQuickPulseServiceClient
    {
        public int PingCount { get; private set; }

        public int SampleCount { get; private set; }

        public bool ReturnValueFromPing { get; set; }

        public bool ReturnValueFromSubmitSample { get; set; }

        public void Reset()
        {
            this.PingCount = 0;
            this.SampleCount = 0;
        }

        public bool Ping()
        {
            this.PingCount++;

            return this.ReturnValueFromPing;
        }

        public bool SubmitSample(QuickPulseDataSample sample)
        {
            this.SampleCount++;

            return this.ReturnValueFromSubmitSample;
        }
    }
}