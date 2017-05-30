namespace Microsoft.ApplicationInsights.DataContracts
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using Assert = Xunit.Assert;
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;

    [TestClass]
    public class PerformanceCounterTelemetryTest
    {
#pragma warning disable 618

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            PerformanceCounterTelemetry original = new PerformanceCounterTelemetry();
            original.CategoryName = null;
            original.CounterName = null;
            original.InstanceName = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PerformanceCounterTelemetry, AI.MetricData>(original);

            Assert.Equal(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void ContextPropertiesUsedAsTelemetryItemProperties()
        {
            PerformanceCounterTelemetry item = new PerformanceCounterTelemetry();

            item.Context.Properties["a"] = "b";

            Assert.Equal("b", item.Properties["a"]);
        }

        [TestMethod]
        public void PerformanceCounterTelemetryDeepCloneCopiesAllProperties()
        {
            PerformanceCounterTelemetry item = new PerformanceCounterTelemetry("someCategory", "someCounter", "an instance", 15.7);
            item.Timestamp = DateTimeOffset.Now;
            item.Properties.Add("p1", "p1Val");

            PerformanceCounterTelemetry other = item.DeepClone();

            CompareLogic deepComparator = new CompareLogic();

            var result = deepComparator.Compare(item, other);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
#pragma warning restore 618
    }
}
