namespace Unit.Tests
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class QuickPulseTimeProviderMock : QuickPulseTimeProvider
    {
        private DateTime now = DateTime.UtcNow;

        public override DateTime UtcNow
        {
            get
            {
                return this.now;
            }
        }

        public void FastForward(TimeSpan span)
        {
            this.now += span;
        }
    }
}