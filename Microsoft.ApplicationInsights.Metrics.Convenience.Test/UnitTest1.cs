using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.ApplicationInsights.Metrics.Convenience.Test
{
    /// <summary />
    [TestClass]
    public class UnitTest1
    {
        /// <summary />
        [TestMethod]
        public void TestMethod1()
        {
            TelemetryClient aiClient = new TelemetryClient();

            Metric cowsSold = aiClient.GetMetric("Cows Sold");
            cowsSold.TrackValue(42);


            aiClient.GetMetric("Cows Sold").TrackValue(18);


            Metric itemsInQueue = aiClient.GetMetric("Items in Queue", MetricConfiguration.CounterUInt32);

            itemsInQueue.TrackValue(5);     // 5
            itemsInQueue.TrackValue(3);     // 8
            itemsInQueue.TrackValue(-4);    // 4
            itemsInQueue.TrackValue(1);     // 5
            itemsInQueue.TrackValue(-2);    // 3


            Metric horsesSold = aiClient.GetMetric("Horses sold", "Gender", "Color", MetricConfiguration.MeasurementDouble);

            horsesSold.TrackValue(42);
            bool canTrack = horsesSold.TryTrackValue(18, "Female", "Black");
            canTrack |= horsesSold.TryTrackValue(25, "Female", "White");

            if (! canTrack)
            {
                throw new ApplicationException("Could not track all values (dimension capping?).");
            }


            MetricSeries femaleBlackHorses;
            bool hasSeries = horsesSold.TryGetDataSeries(out femaleBlackHorses, "Female", "Black");

            femaleBlackHorses.TrackValue("125");

        }



























































        /// <summary />
        [TestMethod]
        public void TestMethod2()
        {
        }
        }
}
