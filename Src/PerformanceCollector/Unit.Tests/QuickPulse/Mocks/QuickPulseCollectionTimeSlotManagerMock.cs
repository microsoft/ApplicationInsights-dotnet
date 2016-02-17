namespace Unit.Tests
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;

    internal class QuickPulseCollectionTimeSlotManagerMock : QuickPulseCollectionTimeSlotManager
    {
        private readonly TimeSpan interval;

        public QuickPulseCollectionTimeSlotManagerMock(QuickPulseTimings timings)
        {
            this.interval = timings.CollectionInterval;
        }

        public override DateTime GetNextCollectionTimeSlot(DateTime currentTime)
        {
            return currentTime + this.interval;
        }
    }
}