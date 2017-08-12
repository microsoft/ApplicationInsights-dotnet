namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
#if !NETCOREAPP1_1
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;
#endif

    [TestClass]
    public class EventTelemetryTest
    {
        [TestMethod]
        public void EventTelemetryIsPublic()
        {
            Assert.IsTrue(typeof(EventTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void EventTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<EventTelemetry, AI.EventData>();
            test.Run();
        }

        [TestMethod]
        public void EventTelemetryReturnsNonNullContext()
        {
            EventTelemetry item = new EventTelemetry();
            Assert.IsNotNull(item.Context);
        }
        
        [TestMethod]
        public void EventTelemetrySuppliesParameterizedConstructorToSimplifyCreatingEventWithGivenNameInAdvancedScenarios()
        {
            var @event = new EventTelemetry("Test Name");
            Assert.AreEqual("Test Name", @event.Name);
        }

        [TestMethod]
        public void MetricsReturnsEmptyDictionaryByDefaultToPreventNullReferenceExceptions()
        {
            var @event = new EventTelemetry();
            IDictionary<string, double> metrics = @event.Metrics;
            Assert.IsNotNull(metrics);
        }

        [TestMethod]
        public void EventTelemetrySerializesToJsonCorrectly()
        {
            var expected = new EventTelemetry();
            expected.Name = "Test Event";
            expected.Properties["Test Property"] = "Test Value";
            expected.Metrics["Test Property"] = 4.2;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, AI.EventData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.AreEqual(AI.ItemType.Event, item.name);
            Assert.AreEqual(typeof(AI.EventData).Name, item.data.baseType);
            Assert.AreEqual(2, item.data.baseData.ver);
            Assert.AreEqual(expected.Name, item.data.baseData.name);
            Assert.AreEqual(expected.Metrics.ToArray(), item.data.baseData.measurements.ToArray());
            Assert.AreEqual(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            EventTelemetry original = new EventTelemetry();
            original.Name = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, AI.EventData>(original);

            Assert.AreEqual(2, item.data.baseData.ver);
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

            Assert.AreEqual(new string('Z', Property.MaxEventNameLength), telemetry.Name);

            Assert.AreEqual(2, telemetry.Properties.Count);
            string[] keys = telemetry.Properties.Keys.OrderBy(s => s).ToArray();
            string[] values = telemetry.Properties.Values.OrderBy(s => s).ToArray();
            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength), keys[1]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), values[1]);
            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength - 3) + "1", keys[0]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), values[0]);

            Assert.AreEqual(2, telemetry.Metrics.Count);
            keys = telemetry.Metrics.Keys.OrderBy(s => s).ToArray();
            Assert.AreEqual(new string('Y', Property.MaxDictionaryNameLength), keys[1]);
            Assert.AreEqual(new string('Y', Property.MaxDictionaryNameLength - 3) + "1", keys[0]);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new EventTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual("n/a", telemetry.Name);           
        }

        [TestMethod]
        public void EventTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new EventTelemetry();

            Assert.IsNotNull(telemetry as ISupportSampling);
        }

#if !NETCOREAPP1_1
        [TestMethod]
        public void EventTelemetryDeepCloneCopiesAllProperties()
        {
            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Name = "Test Event";
            eventTelemetry.Properties["Test Property"] = "Test Value";
            eventTelemetry.Metrics["Test Property"] = 4.2;
            EventTelemetry other = (EventTelemetry)eventTelemetry.DeepClone();

            CompareLogic deepComparator = new CompareLogic();

            var result = deepComparator.Compare(eventTelemetry, other);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
#endif

        [TestMethod]
        public void EventTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new EventTelemetry("my event");
            ((ISupportSampling)telemetry).SamplingPercentage = 10;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, AI.EventData>(telemetry);

            Assert.AreEqual(10, item.sampleRate);
        }
    }
}
