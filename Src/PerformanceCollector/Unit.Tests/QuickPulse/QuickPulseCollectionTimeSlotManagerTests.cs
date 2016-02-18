namespace Unit.Tests
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseCollectionTimeSlotManagerTests
    {
        [TestMethod]
        public void QuickPulseCollectionTimeSlotManagerHandlesFirstHalfOfSecond()
        {
            // ARRANGE
            var manager = new QuickPulseCollectionTimeSlotManager();

            var now = new DateTime(2016, 1, 1, 0, 0, 1);
            now = now.AddMilliseconds(499);

            // ACT
            DateTime slot = manager.GetNextCollectionTimeSlot(now);

            // ASSERT
            Assert.AreEqual(now.AddMilliseconds(1), slot);
        }

        [TestMethod]
        public void QuickPulseCollectionTimeSlotManagerHandlesSecondHalfOfSecond()
        {
            // ARRANGE
            var manager = new QuickPulseCollectionTimeSlotManager();

            var now = new DateTime(2016, 1, 1, 0, 0, 1);
            now = now.AddMilliseconds(501);

            // ACT
            DateTime slot = manager.GetNextCollectionTimeSlot(now);

            // ASSERT
            Assert.AreEqual(now.AddSeconds(1).AddMilliseconds(-1), slot);
        }

        [TestMethod]
        public void QuickPulseCollectionTimeSlotManagerHandlesMidPointOfSecond()
        {
            // ARRANGE
            var manager = new QuickPulseCollectionTimeSlotManager();

            var now = new DateTime(2016, 1, 1, 0, 0, 1);
            now = now.AddMilliseconds(500);

            // ACT
            DateTime slot = manager.GetNextCollectionTimeSlot(now);

            // ASSERT
            Assert.AreEqual(now.AddSeconds(1), slot);
        }
    }
}