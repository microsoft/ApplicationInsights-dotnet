namespace User.Namespace.Example01
{
    using System;
    using Microsoft.ApplicationInsights;

    /// <summary>
    /// Most simple cases are one-liners.
    /// This is all possible without even importing an additinal namespace.
    /// </summary>
    public class Sample01
    {
        /// <summary />
        public static void Exec()
        {
            // Recall how you send custom telemetry in other cases, e.g. Events.
            // The following will result in an EventTelemetry object to be send to the cloud right away.
            TelemetryClient client = new TelemetryClient();
            client.TrackEvent("SomethingInterestingHappened");


            // Metrics work very simlar. However, the value is not sent right away.
            // It is aggregated with other values for the same metic, and the resulting summary (aka "aggregate" is sent automatically every minute.
            // To mark this difference, we use a pattern that is similar, but different from the established TrackXxx(..) pattern that sends telemetry right away:
            client.GetMetric("CowsSold").TrackValue(42);

            // We support different kinds of aggrgation types. For now, we include 2: Measurements and Counters.
            // Measurements aggregate tracked values and reduce them to {Count, Sum, Min, Max, StdDev} of all values tracked during each minute. 
            // They are particularly useful if you are measuring something like the number of items sold, the completion time of an operation or similar.

            // Counters are also sent to the cloud each minute.
            // But rather than aggregating values across a time period, they aggregate values across their entire life time (or until you reset them).
            // They are particularly usefuk when you are counting the number of items in a data structure.

            // By default, metrics are aggregated as Measurements. Here is how you can define a metric to be aggregated as a Counter instead:

            Metric itemsInDatastructure = client.GetMetric("ItemsInDatastructure", MetricConfigurations.Counter);

            int itemsAdded = AddItemsToDataStructure();
            itemsInDatastructure.TrackValue(itemsAdded);

            int itemsRemoved = AddItemsToDataStructure();
            itemsInDatastructure.TrackValue(-itemsRemoved);

            // Here is how you can reset a counter:
            ResetDataStructure();
            itemsInDatastructure.GetAllSeries()[0].Value.ResetAggregation();

            // The above example shows a zero-dimensional metric.
            // Metrics can also be multi-dimensional.
            // In the initial version we are supporting up to 2 dimensions, and we will add suppot for more in the future as needed.
            // Here is an example for a one-dimensional metric:

            Metric animalsSold = client.GetMetric("AnimalsSold", "Species");
            animalsSold.TryTrackValue(42, "Pigs");
            animalsSold.TryTrackValue(24, "Horses");

            // The values for Pigs and Horses will be aggregated separately and will result in two aggregates.
            // You can control the of number data series per metric (and thus your resource usage and cost).
            // The default limits are no more than 1000 total data series per metric and no more than 100 different values per dimension.
            // We discuss elsewhere how to change them.
            // We use a common .Net pattern: TryXxx(..) to make sure that the limits are observed.
            // If the limits are already reached, Metric.TryTrackValue(..) will return False and the value will not be tracked. Otherwise it will return True.
            // This is particularly useful if the data for a metric originates from user input, e.g. a file:

            (int count, string species) = ReadSpeciesFromUserInput();

            if (! animalsSold.TryTrackValue(count, species))
            {
                client.TrackTrace($"Data series or dimension cap was reached for metric {animalsSold.MetricId}.");
            }

            // You can inspect a metric object to reason about its current state. For example:
            int currentNumberOfSpecies = animalsSold.GetDimensionValues(1).Count;
        }

        private static void ResetDataStructure()
        {
            throw new NotImplementedException();
        }

        private static (int count, string species) ReadSpeciesFromUserInput()
        {
            throw new NotImplementedException();
        }

        private static int AddItemsToDataStructure()
        {
            throw new NotImplementedException();
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example01
{
    using System;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Importing the <c>Microsoft.ApplicationInsights.Metrics</c> namespace supports some more advanced use cases.
    /// These include:
    ///  - Working with MetricSeries
    ///  - Configuring a metric
    ///  - Working directly with the MetricManager
    ///  
    /// In this example we cover working with MetricSeries.
    /// </summary>
    public class Sample02
    {
        /// <summary />
        public static void Exec()
        {
            // Recall that metric can be multidimensional. For example, assume that we want to track the number of books sold by Genre and by Language.

            TelemetryClient client = new TelemetryClient();
            Metric booksSold = client.GetMetric("BooksSold", "Genre", "Language");
            booksSold.TryTrackValue(10, "Science Fiction", "English");
            booksSold.TryTrackValue(15, "Historic Novels", "English");
            booksSold.TryTrackValue(20, "Epic Tragedy", "Russian");

            // Recall from the prevous example that each of the above TryTrackValue(..) statements will create a
            // new data series and use it to track the specified value.
            // If you use the same dimension values as before, than instead of creating a new series, the system will look up and use an existing series:

            booksSold.TryTrackValue(8, "Science Fiction", "English");   // Now we have 18 Science Fiction books in English


            // If you use certain data series frequently you can avoid this lookup by keeping a reference to it:

            MetricSeries epicTragedyInRussianSold;
            booksSold.TryGetDataSeries(out epicTragedyInRussianSold, "Epic Tragedy", "Russian");
            epicTragedyInRussianSold.TrackValue(6); // Now we have 26 Epic Tragedies in Russian
            epicTragedyInRussianSold.TrackValue(5); // Now we have 31 Epic Tragedies in Russian

            // Notice the "Try" in TryGetDataSeries(..). Recall the previus example where we explained the TryTrackValue(..) pattern.
            // The same reasoning applies here.

            // So Metric is a container of one or more data series.
            // The actual data belongs a specific MetricSeries object and the Metric object is a grouping of one or more series.

            // A zero-dimensional metric has exactly one metric data series:
            Metric cowsSold = client.GetMetric("CowsSold");
            Assert.AreEqual(0, cowsSold.DimensionsCount);

            MetricSeries cowsSoldValues;
            cowsSold.TryGetDataSeries(out cowsSoldValues);
            cowsSoldValues.TrackValue(25);

            // Multi-dimensional metrics can have more data series:
            MetricSeries unspecifiedBooksSold, cookbookInGermanSold;
            booksSold.TryGetDataSeries(out unspecifiedBooksSold);
            booksSold.TryGetDataSeries(out cookbookInGermanSold, "Cookbook", "German");

            // You can get the "special" zero-dimensional series from every metric, regardless of now many dimensions it has.
            // But if you specify any dimension values, you must specify the correct number, otherwise an exception is thrown.

            try
            {
                MetricSeries epicTragediesSold;
                booksSold.TryGetDataSeries(out epicTragediesSold, "Epic Tragedy");
            }
            catch(InvalidOperationException)
            {
                Console.WriteLine("This will always happen since 'booksSold' has 2 dimensions, but we only specified one.");
            }

            //epicTragedyInRussianSold.GetCurrentAggregateUnsafe
            //MetricTelemetry

        }

        private static int AddItemsToDataStructure()
        {
            throw new NotImplementedException();
        }

        private static string ReadSpeciesFromUserInput()
        {
            throw new NotImplementedException();
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------