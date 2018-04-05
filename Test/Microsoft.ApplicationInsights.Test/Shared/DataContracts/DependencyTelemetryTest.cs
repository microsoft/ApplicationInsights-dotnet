namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using KellermanSoftware.CompareNetObjects;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DependencyTelemetryTest
    {
        /// <summary>
        /// The SDKs (and our customers) expect specific default values.
        /// This test is to verify that changes to the schema don't unexpectedly change our public api.
        /// </summary>
        [TestMethod]
        public void VerifyExpectedDefaultValue()
        {
            var defaultDependencyTelemetry = new DependencyTelemetry();
            Assert.AreEqual(true, defaultDependencyTelemetry.Success, "Success is expected to be true");
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializesToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(expected);

            Assert.AreEqual<DateTimeOffset>(expected.Timestamp, DateTimeOffset.Parse(item.time, null, System.Globalization.DateTimeStyles.AssumeUniversal));
            Assert.AreEqual(expected.Sequence, item.seq);
            Assert.AreEqual(expected.Context.InstrumentationKey, item.iKey);
            AssertEx.AreEqual(expected.Context.SanitizedTags.ToArray(), item.tags.ToArray());
            Assert.AreEqual(typeof(AI.RemoteDependencyData).Name, item.data.baseType);

            Assert.AreEqual(expected.Id, item.data.baseData.id);
            Assert.AreEqual(expected.ResultCode, item.data.baseData.resultCode);
            Assert.AreEqual(expected.Name, item.data.baseData.name);
            Assert.AreEqual(expected.Duration, TimeSpan.Parse(item.data.baseData.duration));
            Assert.AreEqual(expected.Type, item.data.baseData.type);

            Assert.AreEqual(expected.Success, item.data.baseData.success);
            AssertEx.AreEqual(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            DependencyTelemetry original = new DependencyTelemetry();
            original.Name = null;
            original.Data = null;
            original.Type = null;
            original.Success = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(original);

            Assert.AreEqual(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializesStructuredIKeyToJsonCorrectlyPreservingPrefixCasing()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry();
            expected.Context.InstrumentationKey = "AIC-" + expected.Context.InstrumentationKey;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(expected);

            Assert.AreEqual(expected.Context.InstrumentationKey, item.iKey);
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializeCommandNameToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry("Select * from Customers where CustomerID=@1");
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(expected);
            AI.RemoteDependencyData dp = item.data.baseData;
            Assert.AreEqual(expected.Data, dp.data);
        }

        [TestMethod]
        public void RemoteDependencyTelemetrySerializeNullCommandNameToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry(null);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(expected);
            AI.RemoteDependencyData dp = item.data.baseData;
            Assert.IsTrue(string.IsNullOrEmpty(dp.data));
        }
        
        [TestMethod]
        public void RemoteDependencyTelemetrySerializeEmptyCommandNameToJson()
        {
            DependencyTelemetry expected = this.CreateRemoteDependencyTelemetry(string.Empty);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(expected);
            AI.RemoteDependencyData dp = item.data.baseData;
            Assert.IsTrue(string.IsNullOrEmpty(dp.data));
        }

        [TestMethod]
        public void DependencyTypeNameDefaultsToEmptyInConstructor()
        {
#pragma warning disable 618
            var dependency = new DependencyTelemetry("name", "command name", DateTimeOffset.Now, TimeSpan.FromSeconds(42), false);

            AssertEx.IsEmpty(dependency.DependencyKind);
#pragma warning restore 618

            AssertEx.IsEmpty(dependency.Type);
        }

        [TestMethod]
        public void SerttingDependencyKindSetsDependencyTypeName()
        {
            DependencyTelemetry expected = new DependencyTelemetry();
#pragma warning disable 618
            expected.DependencyKind = "Http";

            Assert.AreEqual("Http", expected.DependencyKind);
            Assert.AreEqual("Http", expected.DependencyTypeName);
#pragma warning restore 618

        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            DependencyTelemetry telemetry = new DependencyTelemetry();
            telemetry.Name = new string('Z', Property.MaxNameLength + 1);
            telemetry.Data = new string('Y', Property.MaxDataLength + 1);
            telemetry.Type = new string('D', Property.MaxDependencyTypeLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));
            
            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual(new string('Z', Property.MaxNameLength), telemetry.Name);
            Assert.AreEqual(new string('Y', Property.MaxDataLength), telemetry.Data);
            Assert.AreEqual(new string('D', Property.MaxDependencyTypeLength), telemetry.Type);

            Assert.AreEqual(2, telemetry.Properties.Count);
            var t = new SortedList<string, string>(telemetry.Properties);

            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength), t.Keys.ToArray()[1]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), t.Values.ToArray()[1]);
            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength - 3) + "1", t.Keys.ToArray()[0]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), t.Values.ToArray()[0]);

            Assert.AreSame(telemetry.Properties, telemetry.Properties);
        }

        [TestMethod]
        public void DependencyTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new DependencyTelemetry();

            Assert.IsNotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void DependencyTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = this.CreateRemoteDependencyTelemetry("mycommand");
            ((ISupportSampling)telemetry).SamplingPercentage = 10;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<DependencyTelemetry, AI.RemoteDependencyData>(telemetry);

            Assert.AreEqual(10, item.sampleRate);
        }

#if !NETCOREAPP1_1
        [TestMethod]
        public void DependencyTelemetryDeepCloneCopiesAllProperties()
        {
            DependencyTelemetry telemetry = CreateRemoteDependencyTelemetry();
            DependencyTelemetry other = (DependencyTelemetry)telemetry.DeepClone();

            ComparisonConfig comparisonConfig = new ComparisonConfig();
            CompareLogic deepComparator = new CompareLogic(comparisonConfig);

            ComparisonResult result = deepComparator.Compare(telemetry, other);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
#endif

        private DependencyTelemetry CreateRemoteDependencyTelemetry()
        {
            DependencyTelemetry item = new DependencyTelemetry
                                            {                                              
                                                Timestamp = DateTimeOffset.Now,
                                                Sequence = "4:2",
                                                Name = "MyWebServer.cloudapp.net",
                                                Duration = TimeSpan.FromMilliseconds(42),
                                                Success = true,
                                                Id = "DepID",
                                                ResultCode = "200",
                                                Type = "external call"
                                            };
            item.Context.InstrumentationKey = Guid.NewGuid().ToString();
            item.Properties.Add("TestProperty", "TestValue");

            return item;
        }

        private DependencyTelemetry CreateRemoteDependencyTelemetry(string commandName)
        {
            DependencyTelemetry item = this.CreateRemoteDependencyTelemetry();
            item.Data = commandName;
            return item;
        }
    }
}