namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SumUpGaugeTests
    {
        [TestMethod]
        public void SumUpGaugeGetValueAndResetWorking()
        {
            SumUpGauge twoTimesPrivateBytes = new SumUpGauge(
                "twoTimesPrivateBytes", 
                new PerformanceCounterFromJsonGauge(@"\Process(??APP_WIN32_PROC??)\Private Bytes * 2", "privateBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests()), 
                new PerformanceCounterFromJsonGauge(@"\Process(??APP_WIN32_PROC??)\Private Bytes", "privateBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests()));

            PerformanceCounterFromJsonGauge privateBytes = new PerformanceCounterFromJsonGauge(@"\Process(??APP_WIN32_PROC??)\Private Bytes", "privateBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests());

            float expectedValue = privateBytes.GetValueAndReset();
            float actualValue = twoTimesPrivateBytes.GetValueAndReset();

            // twoTimesPrivateBytes is -greater than (privateBytes * 1.85) but lower than (privateBytes * 2.15).
            Assert.IsTrue((expectedValue * 1.85) < actualValue && (expectedValue * 2.15) > actualValue);
        }
    }
}
