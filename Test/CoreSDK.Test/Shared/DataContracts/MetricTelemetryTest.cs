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
    public class MetricTelemetryTest
    {
        [TestMethod]
        public void MetricTelemetryIsPublic()
        {
            Assert.True(typeof(MetricTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void MetricTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<MetricTelemetry, AI.MetricData>();
            test.Run();
        }

        [TestMethod]
        public void EventTelemetryReturnsNonNullContext()
        {
            MetricTelemetry item = new MetricTelemetry();
            Assert.NotNull(item.Context);
        }

        [TestMethod]
        public void MetricTelemetrySuppliesConstructorThatTakesNameAndValueToSimplifyAdvancedScenarios()
        {
            var instance = new MetricTelemetry("Test Metric", 4.2);
            Assert.Equal("Test Metric", instance.Name);
            Assert.Equal(4.2, instance.Value);
        }

        [TestMethod]
        public void MetricTelemetrySuppliesPropertiesForCustomerToSendAggregatedMetric()
        {
            var instance = new MetricTelemetry("Test Metric", 4.2);
            instance.Count = 5;
            instance.Min = 1.2;
            instance.Max = 6.4;
            instance.StandardDeviation = 0.5;
            Assert.Equal(5, instance.Count);
            Assert.Equal(1.2, instance.Min);
            Assert.Equal(6.4, instance.Max);
            Assert.Equal(0.5, instance.StandardDeviation);
        }

        [TestMethod]
        public void MeasurementMetricTelemetrySerializesToJsonCorrectly()
        {
            var expected = new MetricTelemetry();
            expected.Name = "My Page";
            expected.Value = 42;
            expected.Properties.Add("Property1", "Value1");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(item.name, AI.ItemType.Metric);

            Assert.Equal(typeof(AI.MetricData).Name, item.data.baseType);
            Assert.Equal(2, item.data.baseData.ver);
            Assert.Equal(1, item.data.baseData.metrics.Count);
            Assert.Equal(expected.Name, item.data.baseData.metrics[0].name);
            Assert.Equal(AI.DataPointType.Measurement, item.data.baseData.metrics[0].kind);
            Assert.Equal(expected.Value, item.data.baseData.metrics[0].value);
            Assert.False(item.data.baseData.metrics[0].count.HasValue);
            Assert.False(item.data.baseData.metrics[0].min.HasValue);
            Assert.False(item.data.baseData.metrics[0].max.HasValue);
            Assert.False(item.data.baseData.metrics[0].stdDev.HasValue);
            Assert.Equal(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void AggregateMetricTelemetrySerializesToJsonCorrectly()
        {
            var expected = new MetricTelemetry();
            expected.Name = "My Page";
            expected.Value = 42;
            expected.Count = 5;
            expected.Min = 1.2;
            expected.Max = 6.4;
            expected.StandardDeviation = 0.5;
            expected.Properties.Add("Property1", "Value1");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(expected);

            Assert.Equal(typeof(AI.MetricData).Name, item.data.baseType);

            Assert.Equal(2, item.data.baseData.ver);
            Assert.Equal(1, item.data.baseData.metrics.Count);
            Assert.Equal(expected.Name, item.data.baseData.metrics[0].name);
            Assert.Equal(AI.DataPointType.Aggregation, item.data.baseData.metrics[0].kind);
            Assert.Equal(expected.Value, item.data.baseData.metrics[0].value);
            Assert.Equal(expected.Count.Value, item.data.baseData.metrics[0].count.Value);
            Assert.Equal(expected.Min.Value, item.data.baseData.metrics[0].min.Value);
            Assert.Equal(expected.Max.Value, item.data.baseData.metrics[0].max.Value);
            Assert.Equal(expected.StandardDeviation.Value, item.data.baseData.metrics[0].stdDev.Value);

            Assert.Equal(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void MetricTelemetrySerializesStructuredIKeyCorrectlyPreservingCaseOfPrefix()
        {
            var metricTelemetry = new MetricTelemetry();
            metricTelemetry.Context.InstrumentationKey = "AIC-" + Guid.NewGuid().ToString();
            ((ITelemetry)metricTelemetry).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(metricTelemetry);

            Assert.Equal(metricTelemetry.Context.InstrumentationKey, item.iKey);
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            MetricTelemetry telemetry = new MetricTelemetry();
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
            var telemetry = new MetricTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal("n/a", telemetry.Name);
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            MetricTelemetry original = new MetricTelemetry();
            original.Name = null;
            original.Max = null;
            original.Min = null;
            original.Count = null;
            original.StandardDeviation = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(original);

            Assert.Equal(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void SerializeReplacesNaNValueOn0()
        {
            MetricTelemetry original = new MetricTelemetry("test", double.NaN);
            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Value);
        }

        [TestMethod]
        public void SerializeReplacesNaNMinOn0()
        {
            MetricTelemetry original = new MetricTelemetry { Min = double.NaN };
            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Min.Value);
        }

        [TestMethod]
        public void SerializeReplacesNaNMaxOn0()
        {
            MetricTelemetry original = new MetricTelemetry { Max = double.NaN };
            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.Max.Value);
        }

        [TestMethod]
        public void SerializeReplacesNaNStandardDeviationOn0()
        {
            MetricTelemetry original = new MetricTelemetry { StandardDeviation = double.NaN };
            ((ITelemetry)original).Sanitize();

            Assert.Equal(0, original.StandardDeviation.Value);
        }
    }
}
