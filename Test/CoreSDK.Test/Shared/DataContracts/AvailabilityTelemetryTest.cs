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
    using DataPlatformModel = Developer.Analytics.DataCollection.Model.v2;

    [TestClass]
    public class AvailabilityTelemetryTest
    {
        [TestMethod]
        public void AvailabilityTelemetrySerializesToJson()
        {
            AvailabilityTelemetry expected = this.CreateAvailabilityTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<AvailabilityTelemetry, DataPlatformModel.AvailabilityData>(expected);

            Assert.Equal(expected.Timestamp, item.Time);
            Assert.Equal(expected.Sequence, item.Seq);
            Assert.Equal(expected.Context.InstrumentationKey, item.IKey);
            Assert.Equal(expected.Context.Tags.ToArray(), item.Tags.ToArray());
            Assert.Equal(typeof(DataPlatformModel.AvailabilityData).Name, item.Data.BaseType);

            Assert.Equal(expected.Duration, item.Data.BaseData.Duration);
            Assert.Equal(expected.Message, item.Data.BaseData.Message);
            Assert.Equal(expected.Success, (item.Data.BaseData.Result == DataPlatformModel.TestResult.Pass) ? true : false);
            Assert.Equal(expected.RunLocation, item.Data.BaseData.RunLocation);
            Assert.Equal(expected.TestName, item.Data.BaseData.TestName);
            Assert.Equal(expected.Id.ToString(), item.Data.BaseData.TestRunId);
            Assert.Equal(expected.TestTimeStamp, item.Data.BaseData.TestTimeStamp);

            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFieldsForAvailability()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry();
            telemetry.Message = new string('Z', Property.MaxAvailabilityMessageLength + 1);
            telemetry.RunLocation = new string('Y', Property.MaxRunLocationLength + 1);
            telemetry.TestName = new string('D', Property.MaxTestNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(new string('Z', Property.MaxAvailabilityMessageLength), telemetry.Message);
            Assert.Equal(new string('Y', Property.MaxRunLocationLength), telemetry.RunLocation);
            Assert.Equal(new string('D', Property.MaxTestNameLength), telemetry.TestName);

            Assert.Equal(3, telemetry.Properties.Count); //AvailabilityTelemetry sanitize already sets one property which is why this is 3 instead of 2
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);

            Assert.Same(telemetry.Properties, telemetry.Properties);
        }

        [TestMethod]
        public void SanitizeWillAddDefaultFieldsForAvailability()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry
            {
                Message = "Test Message",
                RunLocation = "Test Location",
                TestName = "Test Name",
                TestTimeStamp = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(30),
                Success = true
            };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Properties["FullTestResultAvailable"], "false");
        }

        [TestMethod]
        public void SanitizeWillLeaveCustomMessageForAvailability()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry
            {
                Message = "Test Message",
                RunLocation = "Test Location",
                TestName = "Test Name",
                TestTimeStamp = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(30),
                Success = true
            };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Data.message, "Test Message");
        }

        [TestMethod]
        public void SanitizeWillSetFailedMessageForAvailability()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry
            {
                RunLocation = "Test Location",
                TestName = "Test Name",
                TestTimeStamp = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(30),
                Success = false
            };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Data.message, "Failed");
        }

        [TestMethod]
        public void SanitizeWillSetPassedMessageForAvailability()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry
            {
                RunLocation = "Test Location",
                TestName = "Test Name",
                TestTimeStamp = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(30),
                Success = true
            };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(telemetry.Data.message, "Passed");
        }

        [TestMethod]
        public void ConstructorWillCastSuccessToResult()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry();

            Assert.Equal(telemetry.Data.result, TestResult.Pass);
        }

        [TestMethod]
        public void AssignmentWillCastSuccessToResult()
        {
            AvailabilityTelemetry telemetry = new AvailabilityTelemetry();
            telemetry.Success = false;

            Assert.Equal(telemetry.Data.result, TestResult.Fail);
        }

        private AvailabilityTelemetry CreateAvailabilityTelemetry()
        {
            AvailabilityTelemetry item = new AvailabilityTelemetry
            {
                Message = "Test Message",
                RunLocation = "Test Location",
                TestName = "Test Name",
                TestTimeStamp = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(30),
                Success = true
            };
            item.Context.InstrumentationKey = Guid.NewGuid().ToString();
            item.Properties.Add("TestProperty", "TestValue");

            return item;
        }

        private AvailabilityTelemetry CreateAvailabilityTelemetry(string testName)
        {
            AvailabilityTelemetry item = this.CreateAvailabilityTelemetry();
            item.TestName = testName;
            return item;
        }
    }
}