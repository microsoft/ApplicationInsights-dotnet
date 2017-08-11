namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Assert = Xunit.Assert;

    /// <summary>
    /// Tests for <see cref="JsonSerializer"/>
    /// </summary>
    [TestClass]
    public class JsonSerializerTest
    {
        [TestMethod]
        public void SerializeAsStringMethodSerializesATelemetryCorrectly()
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            string exceptionAsJson = JsonSerializer.SerializeAsString(exceptionTelemetry);

            // Expected: {"name":"Microsoft.ApplicationInsights.Exception","time":"0001-01-01T00:00:00.0000000+00:00","data":{"baseType":"ExceptionData","baseData":{"ver":2,"handledAt":"Unhandled","exceptions":[]}}}
            // Deserialize (Validates a valid JSON string)
            JObject obj = JsonConvert.DeserializeObject<JObject>(exceptionAsJson);

            // Validates 2 random properties
            Assert.NotNull(exceptionAsJson);
            Assert.Equal("Microsoft.ApplicationInsights.Exception", obj["name"].ToString());
        }

        [TestMethod]
        public void SanitizesTelemetryItem()
        {
            string name = new string('Z', 10000);
            EventTelemetry t = new EventTelemetry(name);

            string exceptionAsJson = JsonSerializer.SerializeAsString(t);

            JObject obj = JsonConvert.DeserializeObject<JObject>(exceptionAsJson);
            
            Assert.Equal(512, obj["data"]["baseData"]["name"].ToString().Length);
        }

        [TestMethod]
        public void SanitizesTimestampInIsoFormat()
        {
            EventTelemetry t = new EventTelemetry();
            
            string json = JsonSerializer.SerializeAsString(t);

            Assert.True(json.Contains("\"time\":\"0001-01-01T00:00:00.0000000Z\""));
        }

        [TestMethod]
        public void IfCallConvertToArrayAndThanDeserializeYouGetSameResult()
        {
            byte[] array = JsonSerializer.ConvertToByteArray("test");
            string result = JsonSerializer.Deserialize(array);

            Assert.Equal("test", result);
        }
    }
}