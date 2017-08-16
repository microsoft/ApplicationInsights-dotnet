namespace Microsoft.ApplicationInsights.Tests
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
           this.PerformanceCollectorSanityTest(new WebAppPerformanceCollector(), @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Requests/Sec", "ASP.NET Applications", "Requests/Sec", null);
        }

        [TestMethod]
        [TestCategory("RequiresPerformanceCounters")]
        public void PerformanceCollectorAddRemoveCountersForWebAppTest()
        {
            this.PerformanceCollectorAddRemoveCountersForWebAppTest(new WebAppPerformanceCollector());
        }
    }
}