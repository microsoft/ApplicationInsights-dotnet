namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AvailabilityTelemetryTest
    {
        [TestMethod]
        public void AvailabilityTelemetrySerializesToJson()
        {
            AvailabilityTelemetry expected = this.CreateAvailabilityTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<AvailabilityTelemetry, AI.AvailabilityData>(expected);

            Assert.Equal<DateTimeOffset>(expected.Timestamp, DateTimeOffset.Parse(item.time, null, System.Globalization.DateTimeStyles.AssumeUniversal));
            Assert.Equal(expected.Sequence, item.seq);
            Assert.Equal(expected.Context.InstrumentationKey, item.iKey);
            Assert.Equal(expected.Context.SanitizedTags.ToArray(), item.tags.ToArray());
            Assert.Equal(typeof(AI.AvailabilityData).Name, item.data.baseType);

            Assert.Equal(expected.Duration, TimeSpan.Parse(item.data.baseData.duration));
            Assert.Equal(expected.Message, item.data.baseData.message);
            Assert.Equal(expected.Success, item.data.baseData.success);
            Assert.Equal(expected.RunLocation, item.data.baseData.runLocation);
            Assert.Equal(expected.Name, item.data.baseData.name);
            Assert.Equal(expected.Id.ToString(), item.data.baseData.id);

            Assert.Equal(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFieldsForAvailability()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry();
            telemetry.Message = new string('Z', Property.MaxAvailabilityMessageLength + 1);
            telemetry.RunLocation = new string('Y', Property.MaxRunLocationLength + 1);
            telemetry.Name = new string('D', Property.MaxTestNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(new string('Z', Property.MaxAvailabilityMessageLength), telemetry.Message);
            Assert.Equal(new string('Y', Property.MaxRunLocationLength), telemetry.RunLocation);
            Assert.Equal(new string('D', Property.MaxTestNameLength), telemetry.Name);

            Assert.Equal(2, telemetry.Properties.Count); 
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "1", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);

            Assert.Same(telemetry.Properties, telemetry.Properties);
        }

        [TestMethod]
        public void SanitizeWillLeaveCustomMessageForAvailability()
        {
            AvailabilityTelemetry telemetry = CreateAvailabilityTelemetry();

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Data.message, "Test Message");
        }

        [TestMethod]
        public void SanitizeWillSetFailedMessageForAvailability()
        {
            AvailabilityTelemetry telemetry = CreateAvailabilityTelemetry();
            telemetry.Message = string.Empty;
            telemetry.Success = false;

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Data.message, "Failed");
        }

        [TestMethod]
        public void SanitizeWillSetPassedMessageForAvailability()
        {
            AvailabilityTelemetry telemetry = CreateAvailabilityTelemetry();
            telemetry.Message = string.Empty;

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Data.message, "Passed");
        }

        [TestMethod]
        public void ConstructorWillCastSuccessToResult()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry();

            Assert.Equal(telemetry.Data.success, true);
        }

        [TestMethod]
        public void AssignmentWillCastSuccessToResult()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry();
            telemetry.Success = false;

            Assert.Equal(telemetry.Data.success, false);
        }

        private AvailabilityTelemetry CreateAvailabilityTelemetry()
        {
            AvailabilityTelemetry item = new AvailabilityTelemetry
            {
                Message = "Test Message",
                RunLocation = "Test Location",
                Name = "Test Name",
                Duration = TimeSpan.FromSeconds(30),
                Success = true
            };
            item.Context.InstrumentationKey = Guid.NewGuid().ToString();
            item.Properties.Add("TestProperty", "TestValue");
            item.Sequence = "12";

            return item;
        }

        private AvailabilityTelemetry CreateAvailabilityTelemetry(string testName)
        {
            AvailabilityTelemetry item = this.CreateAvailabilityTelemetry();
            item.Name = testName;
            return item;
        }
    }
}