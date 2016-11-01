namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// WebAppPerformanceCollector tests.
    /// </summary>
    [TestClass]
    public class WebAppPerformanceCollectorTests : PerformanceCollectorTestBase
    { 
        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorSanityTest()
        {
            this.PerformanceCollectorSanityTest(new WebAppPerformanceCollector());
        }
    }
}