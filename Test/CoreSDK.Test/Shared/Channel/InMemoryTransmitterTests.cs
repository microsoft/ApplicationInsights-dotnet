namespace Microsoft.ApplicationInsights.Channel
{
    using System;
#if NET40 || NET45 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;

    public class InMemoryTransmitterTests
    {
        [TestClass]
        public class SendingInterval
        {
            [TestMethod]
            public void DefaultValueIsAppropriateForProductionEnvironmentAndUnitTests()
            {
                var transmitter = new InMemoryTransmitter(new TelemetryBuffer());
                Assert.Equal(TimeSpan.FromSeconds(30), transmitter.SendingInterval);
            }

            [TestMethod]
            public void CanBeChangedByChannelToTunePerformance()
            {
                var transmitter = new InMemoryTransmitter(new TelemetryBuffer());

                var expectedValue = TimeSpan.FromSeconds(42);
                transmitter.SendingInterval = expectedValue;

                Assert.Equal(expectedValue, transmitter.SendingInterval);
            }
        }
    }
}
