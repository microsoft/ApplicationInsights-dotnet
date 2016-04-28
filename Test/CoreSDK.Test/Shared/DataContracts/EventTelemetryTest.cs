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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

    [TestClass]
    public class EventTelemetryTest
    {
        [TestMethod]
        public void EventTelemetryIsPublic()
        {
            Assert.True(typeof(EventTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void EventTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<EventTelemetry, DataPlatformModel.EventData>();
            test.Run();
        }

        [TestMethod]
        public void EventTelemetryReturnsNonNullContext()
        {
            EventTelemetry item = new EventTelemetry();
            Assert.NotNull(item.Context);
        }
        
        [TestMethod]
        public void EventTelemetrySuppliesParameterizedConstructorToSimplifyCreatingEventWithGivenNameInAdvancedScenarios()
        {
            var @event = new EventTelemetry("Test Name");
            Assert.Equal("Test Name", @event.Name);
        }

        [TestMethod]
        public void MetricsReturnsEmptyDictionaryByDefaultToPreventNullReferenceExceptions()
        {
            var @event = new EventTelemetry();
            IDictionary<string, double> metrics = @event.Metrics;
            Assert.NotNull(metrics);
        }

        [TestMethod]
        public void EventTelemetrySerializesToJsonCorrectly()
        {
            var expected = new EventTelemetry();
            expected.Name = "Test Event";
            expected.Properties["Test Property"] = "Test Value";
            expected.Metrics["Test Property"] = 4.2;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, DataPlatformModel.EventData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Event, item.Name);
            Assert.Equal(typeof(DataPlatformModel.EventData).Name, item.Data.BaseType);
            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.Equal(expected.Name, item.Data.BaseData.Name);
            Assert.Equal(expected.Metrics.ToArray(), item.Data.BaseData.Measurements.ToArray());
            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            EventTelemetry original = new EventTelemetry();
            original.Name = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, DataPlatformModel.EventData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            EventTelemetry telemetry = new EventTelemetry();
            telemetry.Name = new string('Z', Property.MaxEventNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'X', 42.0);
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'Y', 42.0);

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(new string('Z', Property.MaxEventNameLength), telemetry.Name);

            Assert.Equal(2, telemetry.Properties.Count);
            string[] keys = telemetry.Properties.Keys.OrderBy(s => s).ToArray();
            string[] values = telemetry.Properties.Values.OrderBy(s => s).ToArray();
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), keys[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), values[1]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", keys[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), values[0]);

            Assert.Equal(2, telemetry.Metrics.Count);
            keys = telemetry.Metrics.Keys.OrderBy(s => s).ToArray();
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength), keys[1]);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength - 3) + "001", keys[0]);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new EventTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();
            
            Assert.Contains("name", telemetry.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", telemetry.Name, StringComparison.OrdinalIgnoreCase);            
        }

        [TestMethod]
        public void EventTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new EventTelemetry();

            Assert.NotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void EventTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new EventTelemetry("my event");
            ((ISupportSampling)telemetry).SamplingPercentage = 10;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, DataPlatformModel.EventData>(telemetry);

            Assert.Equal(10, item.SampleRate);
        }
    }
}
