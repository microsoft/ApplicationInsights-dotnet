namespace Microsoft.ApplicationInsights.DataContracts
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

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
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PerformanceCounterTelemetry, DataPlatformModel.PerformanceCounterData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }
    }
}
