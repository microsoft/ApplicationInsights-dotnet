using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics;
//using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace SomeCustomerNamespace
{
    /// <summary />
    [TestClass]
    public class UnitTest1
    {
        /// <summary />
        [TestMethod]
        public void TestMethod1()
        {
            return;

#pragma warning disable CS0162 // Unreachable code detected
            TelemetryClient aiClient = new TelemetryClient();
#pragma warning restore CS0162 // Unreachable code detected

            Metric cowsSold = aiClient.GetMetric("Cows Sold");
            cowsSold.TrackValue(42);


            aiClient.GetMetric("Cows Sold").TrackValue(18);


            Metric itemsInQueue = aiClient.GetMetric("Items in Queue", MetricConfiguration.Counter);

            itemsInQueue.TrackValue(5);     // 5
            itemsInQueue.TrackValue(3);     // 8
            itemsInQueue.TrackValue(-4);    // 4
            itemsInQueue.TrackValue(1);     // 5
            itemsInQueue.TrackValue(-2);    // 3


            Metric horsesSold = aiClient.GetMetric("Horses sold", "Gender", "Color", MetricConfiguration.Measurement);

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

            //aiClient.GetMetric("Foo").GetConfiguration().
            //femaleBlackHorses.

        }



        void MethodX()
        {

            MetricManager manager = TelemetryConfiguration.Active.Metrics();

            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            MetricSeries series1 = TelemetryConfiguration.Active.Metrics().CreateNewSeries("Cows Sold", config);

            series1.TrackValue(42);

            series1.Context.Properties["Color of Cow"] = "Red";
            series1.Context.Properties["Gender of Cow"] = "Female";

            series1.TrackValue(1);
            series1.TrackValue(1);
            series1.TrackValue(1);

        }

        void MethodY()
        {
            TelemetryClient client = new TelemetryClient();

            client.TrackEvent(null);


            client.GetMetric("Cows Sold").TrackValue(42);
            client.GetMetric("Cows Sold").TrackValue(24);

            Metric m = client.GetMetric("Cows Sold", "Color", "Gender");


            m.TrackValue(42);

            m.TryTrackValue(42, "Red", "Female");
            m.TryTrackValue(42, "Green", "Female");
            m.TryTrackValue(42, "Green", "Male");

        }



















































        /// <summary />
        [TestMethod]
        public void TestMethod2()
        {
        }
    }
}
