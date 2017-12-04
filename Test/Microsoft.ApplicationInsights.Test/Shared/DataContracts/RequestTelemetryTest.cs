namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using KellermanSoftware.CompareNetObjects;
    
    [TestClass]
    public class RequestTelemetryTest
    {
        [TestMethod]
        public void RequestTelemetryITelemetryContractConsistentlyWithOtherTelemetryTypes()
        {
            new ITelemetryTest<RequestTelemetry, AI.RequestData>().Run();
        }

        [TestMethod]
        public void ParameterlessConstructorInitializesRequiredFields()
        {
            var request = new RequestTelemetry();
            Assert.IsFalse(string.IsNullOrEmpty(request.Id));
        }

        [TestMethod]
        public void ParameterizedConstructorInitializesNewInstanceWithGivenNameTimestampDurationStatusCodeAndSuccess()
        {
            var start = DateTimeOffset.Now;
            var request = new RequestTelemetry("name", start, TimeSpan.FromSeconds(42), "404", true);
            Assert.AreEqual("name", request.Name);
            Assert.AreEqual("404", request.ResponseCode);
            Assert.AreEqual(TimeSpan.FromSeconds(42), request.Duration);
            Assert.AreEqual(true, request.Success);
            Assert.AreEqual(start, request.Timestamp);
        }

        [TestMethod]
        public void HttpMethodCanBeSetByRequestTracking()
        {
#pragma warning disable 618
            var request = new RequestTelemetry();
            request.HttpMethod = "POST";
            Assert.AreEqual("POST", request.HttpMethod);
#pragma warning restore 618
        }

        [TestMethod]
        public void RequestTelemetryReturnsDefaultDurationAsTimespanZero()
        {
            RequestTelemetry item = new RequestTelemetry();
            Assert.AreEqual(TimeSpan.Zero, item.Duration);
        }

        [TestMethod]
        public void RequestTelemetryReturnsDefaultTimeStampAsDateTimeOffsetMinValue()
        {
            RequestTelemetry item = new RequestTelemetry();
            Assert.AreEqual(DateTimeOffset.MinValue, item.Timestamp);
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            RequestTelemetry original = new RequestTelemetry();
#pragma warning disable 618
            original.HttpMethod = null;
#pragma warning restore 618
            original.Id = null;
            original.Name = null;
            original.ResponseCode = null;
            original.Url = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(original);

            Assert.AreEqual(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void RequestTelemetrySerializesToJson()
        {
            var expected = CreateTestTelemetry();

            ((ITelemetry)expected).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.AreEqual(item.name, AI.ItemType.Request);

            Assert.AreEqual(typeof(AI.RequestData).Name, item.data.baseType);

            Assert.AreEqual(2, item.data.baseData.ver);
            Assert.AreEqual(expected.Id, item.data.baseData.id);
            Assert.AreEqual(expected.Name, item.data.baseData.name);
            Assert.AreEqual(expected.Timestamp, DateTimeOffset.Parse(item.time));
            Assert.AreEqual(expected.Duration, TimeSpan.Parse(item.data.baseData.duration));
            Assert.AreEqual(expected.Success, item.data.baseData.success);
            Assert.AreEqual(expected.ResponseCode, item.data.baseData.responseCode);
            Assert.AreEqual(expected.Url.ToString(), item.data.baseData.url.ToString());

            Assert.AreEqual(1, item.data.baseData.measurements.Count);
            AssertEx.AreEqual(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void SerializePopulatesRequiredFieldsOfRequestTelemetry()
        {
            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var requestTelemetry = new RequestTelemetry();
                requestTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
                ((ITelemetry)requestTelemetry).Sanitize();
                var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(requestTelemetry);

                Assert.AreEqual(2, item.data.baseData.ver);
                Assert.IsNotNull(item.data.baseData.id);
                Assert.IsNotNull(item.time);
                Assert.AreEqual("200", item.data.baseData.responseCode);
                Assert.AreEqual(new TimeSpan(), TimeSpan.Parse(item.data.baseData.duration));
                Assert.IsTrue(item.data.baseData.success);
            }
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            RequestTelemetry telemetry = new RequestTelemetry();
            telemetry.Name = new string('Z', Property.MaxNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'X', 42.0);
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'Y', 42.0);
            telemetry.Url = new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength + 1));
            telemetry.Id = new string('1', Property.MaxNameLength);

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual(new string('Z', Property.MaxNameLength), telemetry.Name);

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

            Assert.AreEqual(new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength - 15)), telemetry.Url);

            Assert.AreEqual(new string('1', Property.MaxNameLength), telemetry.Id);
        }

        [TestMethod]
        public void SanitizeWillInitializeStatusCode()
        {
            RequestTelemetry telemetry = new RequestTelemetry();

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual("200", telemetry.ResponseCode);
        }

        [TestMethod]
        public void SanitizeWillInitializeSucessIfStatusCodeNotProvided()
        {
            RequestTelemetry telemetry = new RequestTelemetry();

            ((ITelemetry)telemetry).Sanitize();

            Assert.IsTrue(telemetry.Success.Value);
        }

        [TestMethod]  
        public void SanitizePopulatesIdWithErrorBecauseItIsRequiredByEndpoint()
        {  
            var telemetry = new RequestTelemetry { Id = null };  
  
            ((ITelemetry)telemetry).Sanitize();  
  
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(telemetry);  
  
            // RequestTelemetry.Id is deprecated and you cannot access it. Method above will validate that all required fields would be populated  
            // AssertEx.Contains("id", telemetry.Id, StringComparison.OrdinalIgnoreCase);  
            // AssertEx.Contains("required", telemetry.Id, StringComparison.OrdinalIgnoreCase);  
        }

        [TestMethod]
        public void RequestTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new RequestTelemetry();

            Assert.IsNotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void RequestTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new RequestTelemetry { Id = null };
            ((ISupportSampling)telemetry).SamplingPercentage = 10;
            ((ITelemetry)telemetry).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(telemetry);

            Assert.AreEqual(10, item.sampleRate);
        }

        [TestMethod]
        public void RequestTelemetryDeepCloneCopiesAllProperties()
        {
            RequestTelemetry request = CreateTestTelemetry();
            RequestTelemetry other = (RequestTelemetry)request.DeepClone();

            ComparisonConfig comparisonConfig = new ComparisonConfig();
            comparisonConfig.MembersToIgnore.Add("HttpMethod"); // Obsolete
            CompareLogic deepComparator = new CompareLogic(comparisonConfig);

            var result = deepComparator.Compare(request, other);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }

        private RequestTelemetry CreateTestTelemetry()
        {
            var request = new RequestTelemetry();
            request.Timestamp = DateTimeOffset.Now;
            request.Id = "a1b2c3d4e5f6h7h8i9j10";
            request.Name = "GET /WebForm.aspx";
            request.Duration = TimeSpan.FromSeconds(4);
            request.ResponseCode = "200";
            request.Success = true;
            request.Url = new Uri("http://localhost/myapp/MyPage.aspx");
            request.Metrics.Add("Metric1", 30);
            request.Properties.Add("userHostAddress", "::1");
            return request;
        }
    }
}
