namespace IntegrationTests.Tests.TestFramework
{
    using Microsoft.ApplicationInsights.Channel;

    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class TelemetryBag
    {
        public ConcurrentBag<ITelemetry> SentItems { get; } = new ConcurrentBag<ITelemetry>();

        public void Clear() => this.SentItems.Clear();

        public void Add(ITelemetry item) => this.SentItems.Add(item);

        public int Count => this.SentItems.Count;

        public List<T> GetTelemetryOfType<T>()
        {
            List<T> foundItems = new List<T>();
            foreach (var item in this.SentItems)
            {
                if (item is T)
                {
                    foundItems.Add((T)item);
                }
            }

            return foundItems;
        }
    }
}
