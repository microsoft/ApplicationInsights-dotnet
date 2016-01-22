namespace Functional.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Newtonsoft.Json;
    using TelemetryItem = Microsoft.Developer.Analytics.DataCollection.Model.v2.TelemetryItem;

    internal static class TelemetryItemFactory
    {
        public static IList<TelemetryItem> GetTelemetryItems(string content)
        {
            var items = new List<TelemetryItem>();

            if (string.IsNullOrWhiteSpace(content))
            {
                return items;
            }

            var newLines = new [] { "\r\n", "\n" };

            string[] lines = content.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                var envelope = JsonConvert.DeserializeObject<Envelope>(line);
                var item = CreateTelemetryItem(envelope, line);
                items.Add(item);
            }

            return items;
        }

        private static TelemetryItem CreateTelemetryItem(
            Envelope envelope, 
            string content)
        {
            TelemetryItem result;

            switch (envelope.Name)
            {
                case Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Exception:
                {
                    result = JsonConvert.DeserializeObject<TelemetryItem<ExceptionData>>(content);
                    break;
                }

                case Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Request:
                {
                    result = JsonConvert.DeserializeObject<TelemetryItem<RequestData>>(content);
                    break;
                }

                case Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Metric:
                {
                    result = JsonConvert.DeserializeObject<TelemetryItem<MetricData>>(content);
                    break;
                }

                case Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.RemoteDependency:
                {
                    result = JsonConvert.DeserializeObject<TelemetryItem<RemoteDependencyData>>(content);
                    break;
                }

                case Microsoft.Developer.Analytics.DataCollection.Model.v1.ItemType.Message:
                {
                    result = JsonConvert.DeserializeObject<TelemetryItem<MessageData>>(content);
                    break;
                }

                default:
                {
                    throw new InvalidDataException("Unsupported telemetry type");
                }
            }

            return result;
        }
    }
}
