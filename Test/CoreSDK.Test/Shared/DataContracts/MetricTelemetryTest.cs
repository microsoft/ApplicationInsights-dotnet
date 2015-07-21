namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

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
            var test = new ITelemetryTest<MetricTelemetry, DataPlatformModel.MetricData>();
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

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, DataPlatformModel.MetricData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(item.Name, Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Metric);

            Assert.Equal(typeof(DataPlatformModel.MetricData).Name, item.Data.BaseType);
            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.Equal(1, item.Data.BaseData.Metrics.Count);
            Assert.Equal(expected.Name, item.Data.BaseData.Metrics[0].Name);
            Assert.Equal(DataPlatformModel.DataPointType.Measurement, item.Data.BaseData.Metrics[0].Kind);
            Assert.Equal(expected.Value, item.Data.BaseData.Metrics[0].Value);
            Assert.False(item.Data.BaseData.Metrics[0].Count.HasValue);
            Assert.False(item.Data.BaseData.Metrics[0].Min.HasValue);
            Assert.False(item.Data.BaseData.Metrics[0].Max.HasValue);
            Assert.False(item.Data.BaseData.Metrics[0].StdDev.HasValue);
            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
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

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, DataPlatformModel.MetricData>(expected);

            Assert.Equal(typeof(DataPlatformModel.MetricData).Name, item.Data.BaseType);

            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.Equal(1, item.Data.BaseData.Metrics.Count);
            Assert.Equal(expected.Name, item.Data.BaseData.Metrics[0].Name);
            Assert.Equal(DataPlatformModel.DataPointType.Aggregation, item.Data.BaseData.Metrics[0].Kind);
            Assert.Equal(expected.Value, item.Data.BaseData.Metrics[0].Value);
            Assert.Equal(expected.Count.Value, item.Data.BaseData.Metrics[0].Count.Value);
            Assert.Equal(expected.Min.Value, item.Data.BaseData.Metrics[0].Min.Value);
            Assert.Equal(expected.Max.Value, item.Data.BaseData.Metrics[0].Max.Value);
            Assert.Equal(expected.StandardDeviation.Value, item.Data.BaseData.Metrics[0].StdDev.Value);

            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void MetricTelemetrySerializesStructuredIKeyCorrectlyPreservingCaseOfPrefix()
        {
            var metricTelemetry = new MetricTelemetry();
            metricTelemetry.Context.InstrumentationKey = "AIC-" + Guid.NewGuid().ToString();
            ((ITelemetry)metricTelemetry).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, DataPlatformModel.MetricData>(metricTelemetry);

            Assert.Equal(metricTelemetry.Context.InstrumentationKey, item.IKey);
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
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new MetricTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Contains("name", telemetry.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", telemetry.Name, StringComparison.OrdinalIgnoreCase);
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
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, DataPlatformModel.MetricData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }

        [TestMethod]
        public void MetricTelemetryIsNotSubjectToSampling()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            const int ItemsToGenerate = 100;

            for (int i = 0; i < 100; i++)
            {
                client.TrackMetric(new MetricTelemetry("metric", 1.0));
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }
    }
}
