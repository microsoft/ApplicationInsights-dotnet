namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RateCounterTests
    {
        [TestMethod]
        public void RateCounterGaugeGetValueAndResetGetsTheValueFromJson()
        {
            RateCounterGauge privateBytesRate = new RateCounterGauge(@"\Process(??APP_WIN32_PROC??)\Private Bytes", "privateBytes", AzureWebApEnvironmentVariables.App, null, new CacheHelperTests());

            double value = privateBytesRate.GetValueAndReset();
            Assert.IsTrue(value == 0);

            System.Threading.Thread.Sleep(System.TimeSpan.FromSeconds(7));

            value = privateBytesRate.GetValueAndReset();

            Assert.IsTrue(value != 0);
        }
    }
}
