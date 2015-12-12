namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class ClockTest
    {
        [TestMethod]
        public void InstanceReturnsDefaultClockInstanceUsedByProductCode()
        {
            IClock instance = Clock.Instance;
            Assert.IsType<Clock>(instance);
        }

        [TestMethod]
        public void InstanceCanBeReplacedByTestsToControlClock()
        {
            TestableClock.Instance = new StubClock();
            Assert.IsType<StubClock>(Clock.Instance);
        }

        [TestMethod]
        public void InstanceCanBeSetToNullByTestsToRestoreDefaultClockImplementation()
        {
            TestableClock.Instance = null;
            Assert.IsType<Clock>(Clock.Instance);
        }

        [TestMethod]
        public void TimeReturnsHighPrecisionDateTimeOffsetValue()
        {
            IClock clock = Clock.Instance;

            DateTimeOffset timestamp1 = clock.Time;
            new ManualResetEvent(false).WaitOne(TimeSpan.FromTicks(50));
            DateTimeOffset timestamp2 = clock.Time;

            Assert.True(timestamp2 - timestamp1 > TimeSpan.FromTicks(5));
            Assert.True(timestamp2 - timestamp1 < TimeSpan.FromTicks(5000));
        }

        private abstract class TestableClock : Clock
        {
            public static new IClock Instance
            {
                set { Clock.Instance = value; }
            }
        }
    }
}
