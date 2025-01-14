namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerfCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RatioCounterTests
    {
        [TestMethod]
        public void RateCounterGaugeGetValueAndResetGetsTheValueFromJson()
        {
            RawCounterGauge readIoBytes = new RawCounterGauge("readIoBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests());
            RawCounterGauge writeIoBytes = new RawCounterGauge("writeIoBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests());
            RawCounterGauge otherIoBytes = new RawCounterGauge("otherIoBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests());

            RatioCounterGauge readIoBytesRate = new RatioCounterGauge(readIoBytes, writeIoBytes);

            double value1 = readIoBytesRate.Collect();
            Assert.IsTrue(value1 != 0);

           SumUpCountersGauge writeAndOtherBytes = new SumUpCountersGauge(writeIoBytes, otherIoBytes);
           RatioCounterGauge totalReadIoBytesRate = new RatioCounterGauge(readIoBytes, writeAndOtherBytes);
           double value2 = totalReadIoBytesRate.Collect();

            Assert.IsTrue(value2 != 0);
            Assert.IsTrue(value1 >= value2);

           RatioCounterGauge totalReadIoBytesPercentage = new RatioCounterGauge(readIoBytes, writeAndOtherBytes, 100);
           double percentage = totalReadIoBytesPercentage.Collect();
            Assert.IsTrue(percentage != 0);
            Assert.IsTrue(Math.Abs((value2 * 100) - percentage) < 1);
        }

        [TestMethod]
        public void RatioCounterGaugeDoesNotThrowWithNullGauges()
        {
            RatioCounterGauge readIoBytesRate = new RatioCounterGauge(null, null);
            readIoBytesRate.Collect();
        }
    }
}
