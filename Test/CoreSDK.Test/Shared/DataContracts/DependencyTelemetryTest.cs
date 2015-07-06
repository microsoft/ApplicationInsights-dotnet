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
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using BondDependencyKind = Extensibility.Implementation.External.DependencyKind;
    using DataPlatformModel = Developer.Analytics.DataCollection.Model.v2;
    
    [TestClass]
    public class DependencyTelemetryTest
    {
        [TestMethod]
        public void RemoteDependencyTelemetrySerializesToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>(expected);

            Assert.Equal(expected.Timestamp, item.Time);
            Assert.Equal(expected.Sequence, item.Seq);
            Assert.Equal(expected.Context.InstrumentationKey, item.IKey);
            Assert.Equal(expected.Context.Tags.ToArray(), item.Tags.ToArray());
            Assert.Equal(typeof(DataPlatformModel.RemoteDependencyData).Name, item.Data.BaseType);

            Assert.Equal(expected.Name, item.Data.BaseData.Name);
            Assert.Equal(DataPlatformModel.DataPointType.Aggregation, item.Data.BaseData.Kind);
            Assert.Equal(expected.Duration.TotalMilliseconds, item.Data.BaseData.Value);
            Assert.Equal(expected.DependencyKind.ToString(), item.Data.BaseData.DependencyKind.ToString());
            Assert.Equal(expected.DependencyTypeName, item.Data.BaseData.DependencyTypeName);

            Assert.Equal(expected.Success, item.Data.BaseData.Success);
            Assert.Equal(expected.Async, item.Data.BaseData.Async);
            
            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            DependencyTelemetry original = new DependencyTelemetry();
            original.Name = null;
            original.CommandName = null;
            original.DependencyTypeName = null;
            original.Success = null;
            original.Async = null;
            original.DependencyKind = null;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializesStructuredIKeyToJsonCorrectlyPreservingPrefixCasing()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry();
            expected.Context.InstrumentationKey = "AIC-" + expected.Context.InstrumentationKey;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>(expected);

            Assert.Equal(expected.Context.InstrumentationKey, item.IKey);
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializeCommandNameToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry("Select * from Customers where CustomerID=@1");
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>(expected);
            DataPlatformModel.RemoteDependencyData dp = item.Data.BaseData;
            Assert.Equal(expected.CommandName, dp.CommandName);
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializeNullCommandNameToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry(null);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>(expected);
            DataPlatformModel.RemoteDependencyData dp = item.Data.BaseData;
            Assert.Null(dp.CommandName);
        }
        
        [TestMethod]
        public void RemoteDependencyTelemetrySerializeEmptyCommandNameToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry(string.Empty);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>(expected);
            DataPlatformModel.RemoteDependencyData dp = item.Data.BaseData;
            Assert.Null(dp.CommandName);
        }

        [TestMethod]
        public void DependencyKindDefaultsToOtherInConstructor()
        {
            var dependency = new DependencyTelemetry("name", "command name", DateTimeOffset.Now, TimeSpan.FromSeconds(42), false);

            Assert.Equal("Other", dependency.DependencyKind);
        }

        [TestMethod]
        public void DependencyKindDefaultsToOtherIfNullIsPassed()
        {
            DependencyTelemetry expected = new DependencyTelemetry();
            expected.DependencyKind = null;

            Assert.Equal(BondDependencyKind.Other.ToString(), expected.DependencyKind);
        }

        [TestMethod]
        public void DependencyKindDefaultsToOtherIfUnexpectedValuePassed()
        {
            DependencyTelemetry expected = new DependencyTelemetry();
            expected.DependencyKind = "badValue";

            Assert.Equal(BondDependencyKind.Other.ToString(), expected.DependencyKind);
        }

        [TestMethod]
        public void DependencyKindIsSetIfValueCanBeParsedFromEnum()
        {
            DependencyTelemetry expected = new DependencyTelemetry();
            expected.DependencyKind = BondDependencyKind.Http.ToString();

            Assert.Equal(BondDependencyKind.Http.ToString(), expected.DependencyKind);
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            DependencyTelemetry telemetry = new DependencyTelemetry();
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

            Assert.Same(telemetry.Properties, telemetry.Properties);
        }

        [TestMethod]
        public void DependencyTelemetryIsSubjectToSampling()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            const int ItemsToGenerate = 100;

            for (int i = 0; i < 100; i++)
            {
                client.Track(new DependencyTelemetry());
            }

            Assert.True(sentTelemetry.Count > 0);
            Assert.True(sentTelemetry.Count < ItemsToGenerate);
        }

        /// <summary>
        /// Serialization/deserialization is broken at this point in DC data model.
        /// Will need to address in the future.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void DependencyTelemetryImplementsISupportSamplingContract()
        {
            var test = new ISupportSamplingTest<DependencyTelemetry, DataPlatformModel.RemoteDependencyData>();
            test.Run();
        }

        private DependencyTelemetry CreateRemoteDependencyTelemetry()
        {
            DependencyTelemetry item = new DependencyTelemetry
                                            {                                              
                                                Timestamp = DateTimeOffset.Now,
                                                Sequence = "4:2",
                                                Name = "MyWebServer.cloudapp.net",
                                                DependencyKind = BondDependencyKind.SQL.ToString(),
                                                Duration = TimeSpan.FromMilliseconds(42),
                                                Async = false,
                                                Success = true,
                                                DependencyTypeName = "external call"
                                            };
            item.Context.InstrumentationKey = Guid.NewGuid().ToString();
            item.Properties.Add("TestProperty", "TestValue");

            return item;
        }

        private DependencyTelemetry CreateRemoteDependencyTelemetry(string commandName)
        {
            DependencyTelemetry item = this.CreateRemoteDependencyTelemetry();
            item.CommandName = commandName;
            return item;
        }
    }
}