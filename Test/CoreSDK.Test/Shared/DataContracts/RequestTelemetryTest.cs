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
    using Assert = Xunit.Assert;
    
    
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
            Assert.False(string.IsNullOrEmpty(request.Id));
        }

        [TestMethod]
        public void ParameterizedConstructorInitializesNewInstanceWithGivenNameTimestampDurationStatusCodeAndSuccess()
        {
            var start = DateTimeOffset.Now;
            var request = new RequestTelemetry("name", start, TimeSpan.FromSeconds(42), "404", true);
            Assert.Equal("name", request.Name);
            Assert.Equal("404", request.ResponseCode);
            Assert.Equal(TimeSpan.FromSeconds(42), request.Duration);
            Assert.Equal(true, request.Success);
            Assert.Equal(start, request.Timestamp);
        }

        [TestMethod]
        public void HttpMethodCanBeSetByRequestTracking()
        {
#pragma warning disable 618
            var request = new RequestTelemetry();
            request.HttpMethod = "POST";
            Assert.Equal("POST", request.HttpMethod);
#pragma warning restore 618
        }

        [TestMethod]
        public void RequestTelemetryReturnsDefaultDurationAsTimespanZero()
        {
            RequestTelemetry item = new RequestTelemetry();
            Assert.Equal(TimeSpan.Zero, item.Duration);
        }

        [TestMethod]
        public void RequestTelemetryReturnsDefaultTimeStampAsDateTimeOffsetMinValue()
        {
            RequestTelemetry item = new RequestTelemetry();
            Assert.Equal(DateTimeOffset.MinValue, item.Timestamp);
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

            Assert.Equal(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void RequestTelemetrySerializesToJson()
        {
            var expected = new RequestTelemetry();
            expected.Timestamp = DateTimeOffset.Now;
            expected.Id = "a1b2c3d4e5f6h7h8i9j10";
            expected.Name = "GET /WebForm.aspx";
            expected.Duration = TimeSpan.FromSeconds(4);
            expected.ResponseCode = "200";
            expected.Success = true;
            expected.Url = new Uri("http://localhost/myapp/MyPage.aspx");
            expected.Metrics.Add("Metric1", 30);
            expected.Properties.Add("userHostAddress", "::1");

            ((ITelemetry)expected).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(item.name, AI.ItemType.Request);

            Assert.Equal(typeof(AI.RequestData).Name, item.data.baseType);

            Assert.Equal(2, item.data.baseData.ver);
            Assert.Equal(expected.Id, item.data.baseData.id);
            Assert.Equal(expected.Name, item.data.baseData.name);
            Assert.Equal(expected.Timestamp, DateTimeOffset.Parse(item.time));
            Assert.Equal(expected.Duration, TimeSpan.Parse(item.data.baseData.duration));
            Assert.Equal(expected.Success, item.data.baseData.success);
            Assert.Equal(expected.ResponseCode, item.data.baseData.responseCode);
            Assert.Equal(expected.Url.ToString(), item.data.baseData.url.ToString(), StringComparer.OrdinalIgnoreCase);

            Assert.Equal(1, item.data.baseData.measurements.Count);
            Assert.Equal(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
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

                Assert.Equal(2, item.data.baseData.ver);
                Assert.NotNull(item.data.baseData.id);
                Assert.NotNull(item.time);
                Assert.Equal("200", item.data.baseData.responseCode);
                Assert.Equal(new TimeSpan(), TimeSpan.Parse(item.data.baseData.duration));
                Assert.True(item.data.baseData.success);
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

            Assert.Equal(new string('Z', Property.MaxNameLength), telemetry.Name);

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

            Assert.Equal(new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength - 15)), telemetry.Url);

            Assert.Equal(new string('1', Property.MaxNameLength), telemetry.Id);
        }

        [TestMethod]
        public void SanitizeWillInitializeStatusCode()
        {
            RequestTelemetry telemetry = new RequestTelemetry();

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal("200", telemetry.ResponseCode);
        }

        [TestMethod]
        public void SanitizeWillInitializeSucessIfStatusCodeNotProvided()
        {
            RequestTelemetry telemetry = new RequestTelemetry();

            ((ITelemetry)telemetry).Sanitize();

            Assert.True(telemetry.Success.Value);
        }

        [TestMethod]
        public void SanitizeWillInitializeSuccessIfStatusCodeNaN()
        {
            RequestTelemetry telemetry = new RequestTelemetry();
            telemetry.ResponseCode = "NaN";

            ((ITelemetry)telemetry).Sanitize();

            Assert.True(telemetry.Success.Value);
        }

        [TestMethod]
        public void SanitizeWillInitializeSuccessIfStatusCodeLess400()
        {
            RequestTelemetry telemetry = new RequestTelemetry();
            telemetry.ResponseCode = "300";

            ((ITelemetry)telemetry).Sanitize();

            Assert.True(telemetry.Success.Value);
        }

        [TestMethod]
        public void SanitizeWillInitializeSuccessIfStatusCode401()
        {
            RequestTelemetry telemetry = new RequestTelemetry();
            telemetry.ResponseCode = "300";

            ((ITelemetry)telemetry).Sanitize();

            Assert.True(telemetry.Success.Value);
        }

        [TestMethod]
        public void SanitizeWillInitializeSuccessIfStatusCode500()
        {
            RequestTelemetry telemetry = new RequestTelemetry();
            telemetry.ResponseCode = "500";

            ((ITelemetry)telemetry).Sanitize();

            Assert.False(telemetry.Success.Value);
        }

        [TestMethod]  
        public void SanitizePopulatesIdWithErrorBecauseItIsRequiredByEndpoint()
        {  
            var telemetry = new RequestTelemetry { Id = null };  
  
            ((ITelemetry)telemetry).Sanitize();  
  
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(telemetry);  
  
            // RequestTelemetry.Id is deprecated and you cannot access it. Method above will validate that all required fields would be populated  
            // Assert.Contains("id", telemetry.Id, StringComparison.OrdinalIgnoreCase);  
            // Assert.Contains("required", telemetry.Id, StringComparison.OrdinalIgnoreCase);  
        }

        [TestMethod]
        public void RequestTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new RequestTelemetry();

            Assert.NotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void RequestTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new RequestTelemetry { Id = null };
            ((ISupportSampling)telemetry).SamplingPercentage = 10;
            ((ITelemetry)telemetry).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, AI.RequestData>(telemetry);

            Assert.Equal(10, item.sampleRate);
        }
    }
}
