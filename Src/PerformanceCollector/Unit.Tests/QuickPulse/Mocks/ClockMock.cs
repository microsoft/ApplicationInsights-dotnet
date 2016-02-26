namespace Unit.Tests
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;

    internal class ClockMock : Clock
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;

        public override DateTimeOffset UtcNow
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