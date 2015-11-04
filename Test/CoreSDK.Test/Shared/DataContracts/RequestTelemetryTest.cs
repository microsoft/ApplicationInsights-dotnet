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
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;
    
    [TestClass]
    public class RequestTelemetryTest
    {
        [TestMethod]
        public void RequestTelemetryITelemetryContractConsistentlyWithOtherTelemetryTypes()
        {
            new ITelemetryTest<RequestTelemetry, DataPlatformModel.RequestData>().Run();
        }

        [TestMethod]
        public void ParameterlessConstructorInitializesRequiredFields()
        {
            var request = new RequestTelemetry();
            Assert.False(string.IsNullOrEmpty(request.Id));
            Assert.Equal("200", request.ResponseCode);
            Assert.Equal(true, request.Success);
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
            var request = new RequestTelemetry();
            request.HttpMethod = "POST";
            Assert.Equal("POST", request.HttpMethod);
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
            original.HttpMethod = null;
            original.Id = null;
            original.Name = null;
            original.ResponseCode = null;
            original.Url = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, DataPlatformModel.RequestData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
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
            expected.HttpMethod = "GET";
            expected.Metrics.Add("Metric1", 30);
            expected.Properties.Add("userHostAddress", "::1");

            ((ITelemetry)expected).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, DataPlatformModel.RequestData>(expected);

            // NOTE: It's correct that we use the v1 name here, and therefore we test against it.
            Assert.Equal(item.Name, Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Request);

            Assert.Equal(typeof(DataPlatformModel.RequestData).Name, item.Data.BaseType);

            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.Equal(expected.Id, item.Data.BaseData.Id);
            Assert.Equal(expected.Name, item.Data.BaseData.Name);
            Assert.Equal(expected.Timestamp, item.Data.BaseData.StartTime);
            Assert.Equal(expected.Duration, item.Data.BaseData.Duration);
            Assert.Equal(expected.Success, item.Data.BaseData.Success);
            Assert.Equal(expected.ResponseCode, item.Data.BaseData.ResponseCode);
            Assert.Equal(expected.Url.ToString(), item.Data.BaseData.Url.ToString(), StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expected.HttpMethod, item.Data.BaseData.HttpMethod);

            Assert.Equal(1, item.Data.BaseData.Measurements.Count);
            Assert.Equal(expected.Metrics, item.Data.BaseData.Measurements);
            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void SerializePopulatesRequiredFieldsOfRequestTelemetry()
        {
            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var requestTelemetry = new RequestTelemetry();
                requestTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
                ((ITelemetry)requestTelemetry).Sanitize();
                var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, DataPlatformModel.RequestData>(requestTelemetry);

                Assert.Equal(2, item.Data.BaseData.Ver);
                Assert.NotNull(item.Data.BaseData.Id);
                Assert.NotNull(item.Data.BaseData.StartTime);
                Assert.Equal("200", item.Data.BaseData.ResponseCode);
                Assert.Equal(new TimeSpan(), item.Data.BaseData.Duration);
                Assert.True(item.Data.BaseData.Success);
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
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);

            Assert.Equal(2, telemetry.Metrics.Count);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength), telemetry.Metrics.Keys.ToArray()[0]);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Metrics.Keys.ToArray()[1]);

            Assert.Equal(new Uri("http://foo.com/" + new string('Y', Property.MaxUrlLength - 15)), telemetry.Url);

            Assert.Equal(new string('1', Property.MaxNameLength), telemetry.Id);
        }
  
        [TestMethod]  
        public void SanitizePopulatesIdWithErrorBecauseItIsRequiredByEndpoint()
        {  
            var telemetry = new RequestTelemetry { Id = null };  
  
            ((ITelemetry)telemetry).Sanitize();  
  
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, DataPlatformModel.RequestData>(telemetry);  
  
            // RequestTelemetry.Id is deprecated and you cannot access it. Method above will validate that all required fields would be populated  
            // Assert.Contains("id", telemetry.Id, StringComparison.OrdinalIgnoreCase);  
            // Assert.Contains("required", telemetry.Id, StringComparison.OrdinalIgnoreCase);  
        }

    [TestMethod]
        public void SanitizePopulatesResponseCodeWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new RequestTelemetry { ResponseCode = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.Contains("responseCode", telemetry.ResponseCode, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("required", telemetry.ResponseCode, StringComparison.OrdinalIgnoreCase);
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

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<RequestTelemetry, DataPlatformModel.RequestData>(telemetry);

            Assert.Equal(10, item.SampleRate);
        }
    }
}
