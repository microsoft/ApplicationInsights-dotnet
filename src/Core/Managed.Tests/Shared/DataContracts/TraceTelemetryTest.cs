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
    public class TraceTelemetryTest
    {
        [TestMethod]
        public void ClassIsPublic()
        {
            Assert.True(typeof(TraceTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void TraceTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<TraceTelemetry, DataPlatformModel.MessageData>();
            test.Run();
        }

        [TestMethod]
        public void ConstructorInitializesDefaultTraceTelemetryInstance()
        {
            var item = new TraceTelemetry();
            Assert.NotNull(item.Context);
            Assert.NotNull(item.Properties);
            Assert.Empty(item.Message);
            Assert.Null(item.SeverityLevel);
        }

        [TestMethod]
        public void ConstructorInitializesTraceTelemetryInstanceWithGivenMessage()
        {
            var item = new TraceTelemetry("TestMessage");
            Assert.NotNull(item.Context);
            Assert.NotNull(item.Properties);
            Assert.Equal("TestMessage", item.Message);
            Assert.Null(item.SeverityLevel);
        }

        [TestMethod]
        public void ConstructorInitializesTraceTelemetryInstanceWithGivenMessageAndSeverityLevel()
        {
            var trace = new TraceTelemetry("TestMessage", SeverityLevel.Critical);
            Assert.NotNull(trace.Context);
            Assert.NotNull(trace.Properties);
            Assert.Equal("TestMessage", trace.Message);
            Assert.Equal(SeverityLevel.Critical, trace.SeverityLevel);
        }

        [TestMethod]
        public void TraceTelemetrySerializesToJsonCorrectly()
        {
            var expected = new TraceTelemetry();
            expected.Message = "My Test";
            expected.Properties.Add("Property2", "Value2");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TraceTelemetry, DataPlatformModel.MessageData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(item.Name, Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Message);
            Assert.Equal(typeof(DataPlatformModel.MessageData).Name, item.Data.BaseType);
            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.Equal(expected.Message, item.Data.BaseData.Message);
            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesItemSeverityLevelAsExpectedByEndpoint()
        {
            var expected = new TraceTelemetry { SeverityLevel = SeverityLevel.Information };
            ((ITelemetry)expected).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TraceTelemetry, DataPlatformModel.MessageData>(expected);
            
            Assert.Equal(Developer.Analytics.DataCollection.Model.v2.SeverityLevel.Information, item.Data.BaseData.SeverityLevel.Value);
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            TraceTelemetry telemetry = new TraceTelemetry();
            telemetry.Message = new string('X', Property.MaxMessageLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(new string('X', Property.MaxMessageLength), telemetry.Message);
            Assert.Equal(2, telemetry.Properties.Count);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);
        }

        [TestMethod]
        public void SanitizePopulatesMessageWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new TraceTelemetry { Message = null };

            ((ITelemetry)telemetry).Sanitize();
            
            Assert.Contains("message", telemetry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", telemetry.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void TraceTelemetryIsSubjectToSampling()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            const int ItemsToGenerate = 100;

            for (int i = 0; i < 100; i++)
            {
                client.TrackTrace("message");
            }

            Assert.True(sentTelemetry.Count > 0);
            Assert.True(sentTelemetry.Count < ItemsToGenerate);
        }

        [TestMethod]
        public void TraceTelemetryImplementsISupportSamplingContract()
        {
            var test = new ISupportSamplingTest<TraceTelemetry, DataPlatformModel.MessageData>();
            test.Run();
        }
    }
}
