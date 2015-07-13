namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;    
    using JsonSerializer = Microsoft.ApplicationInsights.Extensibility.Implementation.JsonSerializer;
    using JsonWriter = Microsoft.ApplicationInsights.Extensibility.Implementation.JsonWriter;

    internal static class TelemetryItemTestHelper
    {
        /// <summary>
        /// Serializes and deserializes the telemetry item and returns the results.
        /// </summary>
        internal static DataPlatformModel.TelemetryItem<TelemetryDataType> SerializeDeserializeTelemetryItem<TODO_DELETEME, TelemetryDataType>(ITelemetry telemetry)
        {
            byte[] b = JsonSerializer.Serialize(telemetry, compress: false);
            JObject obj = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(b, 0, b.Length));
            return obj.ToObject<DataPlatformModel.TelemetryItem<TelemetryDataType>>();
        }
    }
}