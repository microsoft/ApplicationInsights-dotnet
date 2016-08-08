namespace Microsoft.ApplicationInsights.DataContracts
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    

    [TestClass]
    public class PerformanceCounterTelemetryTest
    {
        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            PerformanceCounterTelemetry original = new PerformanceCounterTelemetry();
            original.CategoryName = null;
            original.CounterName = null;
            original.InstanceName = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PerformanceCounterTelemetry, AI.PerformanceCounterData>(original);

            Assert.Equal(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void ContextPropertiesUsedAsTelemetryItemProperties()
        {
            PerformanceCounterTelemetry item = new PerformanceCounterTelemetry();

            item.Context.Properties["a"] = "b";

            Assert.Equal("b", item.Properties["a"]);
        }
    }
}
