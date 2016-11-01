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

        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorRefreshTest()
        {
            this.PerformanceCollectorRefreshTest(new WebAppPerformanceCollector());
        }

        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorBadStateTest()
        {
            // Bad State Is not actualy suppported in web apps. When the value is not available, it by default returns zero today.
        }
    }
}