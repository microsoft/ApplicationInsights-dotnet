namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseServiceClientTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseDataAccumulatorManager.ResetInstance();
        }
    }
}