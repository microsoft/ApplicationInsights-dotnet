namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AggregatedMetricTelemetryTest
    {
        [TestMethod]
        public void AggregatedMetricTelemetryIsPublic()
        {
            Assert.True(typeof(AggregatedMetricTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void AggregatedMetricTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<AggregatedMetricTelemetry, AI.MetricData>();
            test.Run();
        }

        [TestMethod]
        public void AggrgatedMetricTelemetryReturnsNonNullContext()
        {
            AggregatedMetricTelemetry item = new AggregatedMetricTelemetry();
            Assert.NotNull(item.Context);
        }

        [TestMethod]
        public void AggregatedMetricTelemetrySuppliesConstructorThatAllowsToFullyPopulateAggregationData()
        {
            var instance = new AggregatedMetricTelemetry("Test Metric", 4, 40, 5, 15, 4.2);

            Assert.Equal("Test Metric", instance.Name);
            Assert.Equal(4, instance.Count);
            Assert.Equal(40, instance.Sum);
            Assert.Equal(5, instance.Min);
            Assert.Equal(15, instance.Max);
            Assert.Equal(4.2, instance.StandardDeviation);
        }

        [TestMethod]
        public void AggregatedMetricTelemetrySuppliesPropertiesForCustomerToSendAggregionData()
        {
            var instance = new AggregatedMetricTelemetry();

            instance.Name = "Test Metric";
            instance.Count = 4;
            instance.Sum = 40;
            instance.Min = 5.0;
            instance.Max = 15.0;
            instance.StandardDeviation = 4.2;

            Assert.Equal("Test Metric", instance.Name);
            Assert.Equal(4, instance.Count);
            Assert.Equal(40, instance.Sum);
            Assert.Equal(5, instance.Min);
            Assert.Equal(15, instance.Max);
            Assert.Equal(4.2, instance.StandardDeviation);
        }

        [TestMethod]
        public void AggregateMetricTelemetrySerializesToJsonCorrectly()
        {
            var expected = new AggregatedMetricTelemetry();

            expected.Name = "Test Metric";
            expected.Sum = 40;
            expected.Count = 4;
            expected.Min = 5.0;
            expected.Max = 15.0;
            expected.StandardDeviation = 4.2;
            expected.Properties.Add("Property1", "Value1");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<AggregatedMetricTelemetry, AI.MetricData>(expected);

            Assert.Equal(typeof(AI.MetricData).Name, item.data.baseType);

            Assert.Equal(2, item.data.baseData.ver);
            Assert.Equal(1, item.data.baseData.metrics.Count);
            Assert.Equal(expected.Name, item.data.baseData.metrics[0].name);
            Assert.Equal(AI.DataPointType.Aggregation, item.data.baseData.metrics[0].kind);
            Assert.Equal(expected.Sum, item.data.baseData.metrics[0].value);
            Assert.Equal(expected.Count, item.data.baseData.metrics[0].count.Value);
            Assert.Equal(expected.Min, item.data.baseData.metrics[0].min.Value);
            Assert.Equal(expected.Max, item.data.baseData.metrics[0].max.Value);
            Assert.Equal(expected.StandardDeviation, item.data.baseData.metrics[0].stdDev.Value);

            Assert.Equal(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void AggregatedMetricTelemetrySerializesStructuredIKeyCorrectlyPreservingCaseOfPrefix()
        {
            var metricTelemetry = new AggregatedMetricTelemetry();
            metricTelemetry.Context.InstrumentationKey = "AIC-" + Guid.NewGuid().ToString();
            ((ITelemetry)metricTelemetry).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<AggregatedMetricTelemetry, AI.MetricData>(metricTelemetry);

            Assert.Equal(metricTelemetry.Context.InstrumentationKey, item.iKey);
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            AggregatedMetricTelemetry telemetry = new AggregatedMetricTelemetry();
            telemetry.Name = new string('Z', Property.MaxNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(new string('Z', Property.MaxNameLength), telemetry.Name);

            Assert.Equal(2, telemetry.Properties.Count);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "1", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new AggregatedMetricTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal("n/a", telemetry.Name);
        }

        [TestMethod]
        public void SerializeReplacesNaNSumOn0()
        {
            AggregatedMetricTelemetry original = new AggregatedMetricTelemetry();
            original.Name = "Test";
            original.Sum = double.NaN;

            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Sum);
        }

        [TestMethod]
        public void SerializeReplacesNaNMinOn0()
        {
            AggregatedMetricTelemetry original = new AggregatedMetricTelemetry();
            original.Name = "Test";
            original.Min = double.NaN;

            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Min);
        }

        [TestMethod]
        public void SerializeReplacesNaNMaxOn0()
        {
            AggregatedMetricTelemetry original = new AggregatedMetricTelemetry();
            original.Name = "Test";
            original.Max = double.NaN;

            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Max);
        }

        [TestMethod]
        public void SerializeReplacesNaNStandardDeviationOn0()
        {
            AggregatedMetricTelemetry original = new AggregatedMetricTelemetry();
            original.Name = "Test";
            original.StandardDeviation = double.NaN;

            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.StandardDeviation);
        }

        [TestMethod]
        public void SerializeReplacesNegativeCountOn0()
        {
            AggregatedMetricTelemetry original = new AggregatedMetricTelemetry();
            original.Name = "Test";
            original.Count = -5; ;

            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Count);
        }
    }
}
