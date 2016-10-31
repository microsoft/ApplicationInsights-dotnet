namespace Unit.Tests
{
    using System;
    using System.Globalization;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CPUPercenageGaugeTests
    {
        [TestMethod]
        public void BasicValidation()
        {
            CPUPercenageGauge gauge = new CPUPercenageGauge(
                "CPU",
                new PerformanceCounterFromJsonGauge(@"\Process(??APP_WIN32_PROC??)\Private Bytes * 2", "userTime", AzureWebApEnvironmentVariables.App, new CacheHelperTests()));

            float value1 = gauge.GetValueAndReset();

            Assert.IsTrue(Math.Abs(value1) < 0.000001);

            Thread.Sleep(TimeSpan.FromSeconds(10));

            float value2 = gauge.GetValueAndReset();
            Assert.IsTrue(
                Math.Abs(value2 - ((24843750 - 24062500.0) / TimeSpan.FromSeconds(10).Ticks * 100.0)) < 0.0001, 
                string.Format(CultureInfo.InvariantCulture, "Actual: {0}, Expected: {1}", value2, (24843750 - 24062500.0) / TimeSpan.FromSeconds(10).Ticks));
        }
    }
}
