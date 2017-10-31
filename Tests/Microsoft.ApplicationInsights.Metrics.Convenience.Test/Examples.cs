namespace User.Namespace.Example01
{
    using System;

    using Microsoft.ApplicationInsights;

    using TraceSeveretyLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel;

    /// <summary>
    /// Most simple cases are one-liners.
    /// This is all possible without even importing an additinal namespace.
    /// </summary>
    public class Sample01
    {
        /// <summary />
        public static void Exec()
        {
            // *** SENDING METRICS ***

            // Recall how you send custom telemetry in other cases, e.g. Events.
            // The following will result in an EventTelemetry object to be send to the cloud right away.
            TelemetryClient client = new TelemetryClient();
            client.TrackEvent("SomethingInterestingHappened");


            // Metrics work very simlar. However, the value is not sent right away.
            // It is aggregated with other values for the same metic, and the resulting summary (aka "aggregate" is sent automatically every minute.
            // To mark this difference, we use a pattern that is similar, but different from the established TrackXxx(..) pattern that sends telemetry right away:
            client.GetMetric("CowsSold").TrackValue(42);

            // *** MEASUREMENTS AND COUNTERS ***

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

            // *** MULTI-DIMENSIONAL METRICS ***

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
                client.TrackTrace($"Data series or dimension cap was reached for metric {animalsSold.MetricId}.", TraceSeveretyLevel.Error);
            }

            // You can inspect a metric object to reason about its current state. For example:
            int currentNumberOfSpecies = animalsSold.GetDimensionValues(1).Count;
        }

        private static void ResetDataStructure()
        {
            // Do stuff
        }

        private static (int count, string species) ReadSpeciesFromUserInput()
        {
            return (18, "Cows");
        }

        private static int AddItemsToDataStructure()
        {
            // Do stuff
            return 5;
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example02
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Metrics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TraceSeveretyLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel;

    /// <summary>
    /// Importing the <c>Microsoft.ApplicationInsights.Metrics</c> namespace supports some more interesting use cases.
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
            // *** ACCESSING METRIC DATA SERIES ***

            // Recall that metrics can be multidimensional. For example, assume that we want to track the number of books sold by Genre and by Language.

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

            // For zero-dimensional metrics you can also get the series in a single line:
            MetricSeries cowsSoldValues2 = cowsSold.GetAllSeries()[0].Value;

            cowsSoldValues2.TrackValue(18); // Now we have 43 cows.
            Assert.AreSame(cowsSoldValues, cowsSoldValues2, "The two series references point to the same obejct");

            // Note, however, that you cannot play this trick with mitli-dimensional series, becasue GetAllSeries() does
            // not provide any guarantees about the ordering of the series it returns.

            // Multi-dimensional metrics can have more than one data series:
            MetricSeries unspecifiedBooksSold, cookbookInGermanSold;
            booksSold.TryGetDataSeries(out unspecifiedBooksSold);
            booksSold.TryGetDataSeries(out cookbookInGermanSold, "Cookbook", "German");

            // You can get the "special" zero-dimensional series from every metric, regardless of now many dimensions it has.
            // But if you specify any dimension values at all, you must specify the correct number, otherwise an exception is thrown.

            try
            {
                MetricSeries epicTragediesSold;
                booksSold.TryGetDataSeries(out epicTragediesSold, "Epic Tragedy");
            }
            catch(InvalidOperationException)
            {
                client.TrackTrace(
                                $"This error will always happen becasue '{nameof(booksSold)}' has 2 dimensions, but we only specified one.",
                                TraceSeveretyLevel.Error);
            }

            // The main purpose of keeping a reference to a metric data series is to use it directly for tracking data.
            // It can improve the performance of your application, exspecially if you are tracking values very frequently, as it avoids 
            // the lookups rececary to first get the metric and then the the series within the metric.

            // *** WORKING WITH THE EMITTED METRIC DATA ***

            // In addition, there are additional operations you you can perform on a series.
            // Most common of them are designed to support interaactive consumption of tracked data.
            // For example, you can reset the values aggregated so far during the current agergation period to the initial state:

            epicTragedyInRussianSold.ResetAggregation();    // Now we have 0 Epic Tragedies in Russian

            // For Measurements, resetting will not make a lot of sense in most cases.
            // However, for Counters this may be necesary oince in a while, for example when you cleared a datascructure, for
            // which you were counting the contained items.

            // Another powerful example for interacting with aggregated metric data is the ability to inspect the aggregation.
            // This means that your application is not just sending metric telemetry for a later inspection, but is able to use its
            // own metrics tpo drive its behavior.
            // For example, the following code determines the currently most popular book and displacy the information:

            MetricAggregate mostPopularBookKind = null;
            foreach (KeyValuePair<string[], MetricSeries> seriesKvp in booksSold.GetAllSeries())
            {
                MetricSeries currentBookInfo = seriesKvp.Value;
                MetricAggregate currentBookKind = currentBookInfo.GetCurrentAggregateUnsafe();

                if (currentBookKind == null)
                {
                    continue;
                }

                if (mostPopularBookKind == null)
                {
                    mostPopularBookKind = currentBookKind;
                }
                else
                {
                    double maxSum = mostPopularBookKind.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Sum, 0);
                    double currentSum = currentBookKind.GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Sum, 0);

                    if (maxSum > currentSum)
                    {
                        mostPopularBookKind = currentBookKind;
                    }
                }
            }

            if (mostPopularBookKind != null)
            {
                DisplayMostPopularBook(mostPopularBookKind);
            }

            // Notice the "...Unsafe" suffix in the MetricSeries.GetCurrentAggregateUnsafe() method.
            // We added it to underline the need for two important considerations when using this method:
            // a) It may return propper objects and nulls in a poorly predictable way:
            //    For performance reasons, we only create internal aggregators if there is any data to aggregate.
            //    Consider a situation where you tracked some values for a Measurement metric. So GetCurrentAggregateUnsafe()
            //    returns a valid object. At any time, the aggregation period (1 minute) could complete. The aggregate
            //    will be "snapped" and sent to the cloud. Now there is no more aggregate until you track more values during
            //    the ongoing aggregation period.
            // b) Aggregator implementations may choose to optimize their multithreaded performance in a way such that the aggregates
            //    do not always reflect the latest state. Data will be synchronized correctly before being sent to the cloud at the end
            //    of the aggregation period, but it may be lagging behind a few milliseconds at other times or it may be inconsistent.
            //    E.g., following a TrackValue(..) invocation the Count statistic of an aggregate may already be updated, but its Sum
            //    statistic may not yet be updated. These errors are small and not statistically significant. However you ushould use
            //    aggregates for what they are - statistical summaries, rather than exact counters.

            // *** ADDITIONAL DATA CONTEXT ***

            // Note that metrics do not usually respet the TelemetryContext of the TelemetryClient used to access the metric.
            // There a detailed discussion of the reasons and workarounds in a latter example. For now, just a clarification:

            TelemetryClient specialClient = new TelemetryClient();
            specialClient.Context.Operation.Name = "Special Operation";
            Metric specialOpsRequestSizeStats = specialClient.GetMetric("Special Operation Request Size");
            int requestSize = GetCurrentRequestSize();
            specialOpsRequestSizeStats.TrackValue(requestSize);

            // Metric aggregates sent by the above specialOpsRequestSizeStats-metric will NOT have their Context.Operation.Name set to "Special Operation".

            // However, MetricSeries objects have their owns data context which WILL be respected. Consider the following code:

            MetricSeries specialOpsRequestSize, someOtherOpsRequestSize;

            client.GetMetric("Request Size", "Operation Name").TryGetDataSeries(out specialOpsRequestSize, "Special Operation");
            client.GetMetric("Request Size", "Operation Name").TryGetDataSeries(out someOtherOpsRequestSize, "Some Other Operation");

            specialOpsRequestSize.AdditionalDataContext.Operation.Name = "Special Operation";
            someOtherOpsRequestSize.AdditionalDataContext.Operation.Name = "Some Other Operation";

            specialOpsRequestSize.TrackValue(120000);
            someOtherOpsRequestSize.TrackValue(64000);

            // In this case, metric aggregates produced by the specialOpsRequestSize-series will have
            // their Context.Operation.Name set to "Special Operation". And the aggregates of the someOtherOpsRequestSize-series
            // will have their Context.Operation.Name set to "Some Other Operation".
        }

        private static int GetCurrentRequestSize()
        {
            // Do stuff
            return 11000;
        }

        private static void DisplayMostPopularBook(MetricAggregate mostPopularBookKind)
        {
            // Do stuff
        }

        private static int AddItemsToDataStructure()
        {
            // Do stuff
            return 3;
        }

        private static string ReadSpeciesFromUserInput()
        {
            // Do stuff
            return "Chicken";
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example03
{
    using System;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Metrics;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TraceSeveretyLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel;

    /// <summary>
    /// In this example we cover configuring a metric.
    /// </summary>
    public class Sample03
    {
        /// <summary />
        public static void Exec()
        {
            // *** SEPARATION OF CONCERNS: TRACKING METRICS AND CONFIGURING AGGREGATIONS ARE INDEPENDENT ***

            // Recall from an earlier example that a metric can be configured for aggregation as a Measurement or as a Counter.
            // A strong architectural conviction of the Metrics SDK is that metrics tracking and metrics aggregation are distinct concepts
            // that must be kept separate. This means that a metric is ALWAYS tracked in the same way:

            TelemetryClient client = new TelemetryClient();
            Metric anyKindOfMetric = client.GetMetric("...");

            anyKindOfMetric.TrackValue(42);

            // If you want to affect the way a metric is aggregated, you need to do this in the one place where the metric is initialized:

            Metric measurementMetric = client.GetMetric("Items Processed per Minute", MetricConfigurations.Measurement);
            Metric counterMetric = client.GetMetric("Items in a Data Structure", MetricConfigurations.Counter);

            measurementMetric.TrackValue(10);
            measurementMetric.TrackValue(20);
            counterMetric.TrackValue(1);
            counterMetric.TrackValue(-1);

            // Note that this is an important and intentional difference to some other metric aggregation libraries
            // that declare a strogly typed metric object class for different aggregators.

            // If you prefer not to cache the metric reference, you can simply avoid specifying the metric configuration in all except the first call.
            // However, you MUST specify a configuration when you initialize the metric for the first time, or we will assume a Measurement.
            // E.g., all three of counterMetric2, counterMetric2a and counterMetric2b below are Counters.
            // (In fact, they are all references to the same object.)

            Metric counterMetric2 = client.GetMetric("Items in a Data Structure 2", MetricConfigurations.Counter);
            Metric counterMetric2a = client.GetMetric("Items in a Data Structure 2");
            Metric counterMetric2b = client.GetMetric("Items in a Data Structure 2", metricConfiguration: null);

            // On contrary, metric3 and metric3a are Measurements, becasue no configuration was specified during the first call:

            Metric metric3 = client.GetMetric("Metric 3");
            Metric metric3a = client.GetMetric("Metric 3", metricConfiguration: null);

            // Be careful: If you specify inconsistent metric configurations, you will get an exception:

            try
            {
                Metric counterMetric2c = client.GetMetric("Items in a Data Structure 2", MetricConfigurations.Measurement);
            }
            catch(ArgumentException)
            {
                client.TrackTrace(
                            "A Metric with the specified Id and dimension names already exists, but it has a configuration"
                          + " that is different from the specified configuration. You may not change configurations once a"
                          + " metric was created for the first time. Either specify the same configuration every time, or"
                          + " specify 'null' during every invocation except the first one. 'Null' will match against any"
                          + " previously specified configuration when retrieving existing metrics, or fall back to"
                          + " MetricConfigurations.Measurement when creating new metrics.",
                            TraceSeveretyLevel.Error);
            }

            // *** CUSTOM METRIC CONFIGURATIONS ***

            // Above we have seen two fixed presets for metric configurations: MetricConfigurations.Measurement and MetricConfigurations.Counter.
            // Both are static objects of class SimpleMetricConfiguration which in turn implements the IMetricConfiguration interface.
            // You can provide your own implementations of IMetricConfiguration if you want to implement your own custom aggregators; that
            // is covered elsewhere.
            // Here, let's focus on creating your own instances of SimpleMetricConfiguration to configure more options.
            // SimpleMetricConfiguration ctor takes some options on how to manage different series within the respective metric and an
            // object of class SimpleMetricSeriesConfiguration : IMetricSeriesConfiguration that specifies aggregation behaviour for
            // each individual series of the metric:

            Metric customConfiguredMeasurement= client.GetMetric(
                                                        "Custom Metric 1",
                                                        new SimpleMetricConfiguration(
                                                                    seriesCountLimit:           1000,
                                                                    valuesPerDimensionLimit:    100,
                                                                    seriesConfig:               new SimpleMetricSeriesConfiguration(
                                                                                                        lifetimeCounter: false,
                                                                                                        restrictToUInt32Values: false)));

            // seriesCountLimit is the max total number of series the metric can contain before TryTrackValue(..) and TryGetDataSeries(..) stop
            // creating new data series nd start returning false.
            // valuesPerDimensionLimit limits the number of distinct values per dimension in a similar manner.
            // lifetimeCounter specifies whether the aggregator for each time series will be replaced at the end of each aggregation cycle (false)
            // or not (true). This corresponds to the Measurement and the Counter aggregations respectively.
            // restrictToUInt32Values can be used to force a metric to consume integer values only. Certain integer-only auto-collected system
            // metrics are stored in the cloud in an optimized, more efficient manner. Custom metrics are currently always stored as doubles.

            // In fact, the above customConfiguredMeasurement is how MetricConfigurations.Measurement is defined by default.

            // If you want to change some of the above configuration values for all metrics in your application without the need to specify 
            // a custom configuration every time, you can do so by using MetricConfigurations.FutureDefaults.
            // Note that this will only affect metrics created after the change:

            Metric someCounter1 = client.GetMetric("Some Counter 1", MetricConfigurations.Counter); 

            MetricConfigurations.FutureDefaults.SeriesCountLimit = 10000;
            MetricConfigurations.FutureDefaults.ValuesPerDimensionLimit = 5000;

            Metric someCounter2 = client.GetMetric("Some Counter 2", MetricConfigurations.Counter);

            // someCounter1 has SeriesCountLimit = 1000 and ValuesPerDimensionLimit = 100.
            // someCounter2 has SeriesCountLimit = 10000 and ValuesPerDimensionLimit = 5000.

            try
            {
                Metric someCounter1a = client.GetMetric("Some Counter 1", MetricConfigurations.Counter);
            }
            catch(ArgumentException)
            {
                // This exception will always occur becasue the configuration object behind MetricConfigurations. has changed
                // when MetricConfigurations.FutureDefaults when was modified.
            }
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example04
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Extensibility;
    
    /// <summary>
    /// In this example we cover working directly with the MetricManager.
    /// </summary>
    public class Sample04
    {
        /// <summary />
        public static void Exec()
        {
            // *** MANUALLY CREATING METRIC SERIES WITHOUT THE CONTEXT OF A METRIC ***

            // In previous examples we learned that a Metric is merely a grouping of one or more MetricSeries, and the actual tracking
            // is performed by the respective MetricSeries.
            // MetricSeries are managed by a class called MetricManager. The MetricManager creates all MetricSeries objects that
            // share a scope, and encapsulates the corresponding aggregation cycles. The default aggregation cycle takes care of
            // sending metrics to the cloud at regular intervals (1 minute). For that, it uses a dedicated managed background thread.
            // This model is aimed at ensuring that metrics are sent regularly even in case of thread pool starvation. However, it can
            // cost significant resources when creating too many custom metric managers (this advanced situation is discussed later).

            // The default scope for a MetricManager is an instance of the Application Insights telemetry pipeline.
            // Other scopes are discussed in later examples.
            // Recall that although in some special sircumstances users can create many instances of the Application Insights telemetry
            // pipeline the normal case is that there is single default pipeline per application, accessible via the static object
            // at TelemetryConfiguration.Active.

            // Expert users can choose to manage their metric series directly, rather than using a Metric container object.
            // In that case they will obtain metric series directly from the MetricManager:

            MetricManager metrics = TelemetryConfiguration.Active.Metrics();

            MetricSeries itemCounter = metrics.CreateNewSeries(
                                                    "Items in Queue",
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));

            itemCounter.TrackValue(1);
            itemCounter.TrackValue(1);
            itemCounter.TrackValue(-1);

            // Note that MetricManager.CreateNewSeries(..) will ALWAYS create a new metric series. It is your responsibility to keep a reference
            // to it so that you can acces it later. If you do not want to worry about keeping that reference, just use Metric.

            // If you choose to useMetricManager directly, you can specify the dimension names and values associated with a new metric series.
            // Note how dimensions can be specified as a dictionary or as an array. On contrary to the Metric class APIs, this approach does not
            // take care of series and dimension capping. You need to take care of it yourself.

            MetricSeries purpleCowsSold = metrics.CreateNewSeries(
                                                    "Animals Sold",
                                                    new Dictionary<string, string>() { ["Species"] = "Cows", ["Color"] = "Purple" },
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));

            MetricSeries yellowHorsesSold = metrics.CreateNewSeries(
                                                    "Animals Sold",
                                                    new[] { new KeyValuePair<string, string>("Species", "Horses"), new KeyValuePair<string, string>("Color", "Yellow") },
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));

            purpleCowsSold.TrackValue(42);
            yellowHorsesSold.TrackValue(132);

            // *** FLUSHING ***

            // MetricManager also allows you to flush all your metrics aggregators and send the current aggregates to the cloud without waiting
            // for the end of the ongoing aggregation period:

            TelemetryConfiguration.Active.Metrics().Flush();
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example05
{
    using System;

    using Microsoft.ApplicationInsights;

    /// <summary>
    /// In this example we cover AggregationScope.
    /// </summary>
    public class Sample05
    {
        /// <summary />
        public static void Exec()
        {
            // *** AGGREGATION SCOPE ***

            // Perviosly we saw that metrics do not use the Telemetry Context of the Telemetry Client used to access them.
            // We learned that MetricSeries.AdditionalDataContext is the best workaround for this limitation.
            // Here, we discuss the reasons for the limitation and other possibile workarounds.

            // Recall the problem description:
            // Metric aggregates sent by the following "Special Operation Request Size"-metric will NOT have their Context.Operation.Name set to "Special Operation".

            TelemetryClient specialClient = new TelemetryClient();
            specialClient.Context.Operation.Name = "Special Operation";
            specialClient.GetMetric("Special Operation Request Size").TrackValue(GetCurrentRequestSize());

            // The reason for that is that by default, metrics are aggregated at the scope of the TelemetryConfiguration pipeline and note at the scope
            // of a particular TelemetryClient. This is becasue of a very common pattern for Application Insights users where a TelemetryClient is created
            // for a small scope:

            {
                // ...
                (new TelemetryClient()).TrackEvent("Something Interesting Happened");
                // ...
            }

            {
                try
                {
                    RunSomeCode();
                }
                catch (ApplicationException apEx)
                {
                    (new TelemetryClient()).TrackException(apEx);
                }
            }

            // We wanted to support this pattern and to allow users to write code like this:

            {
                // ...
                (new TelemetryClient()).GetMetric("Temperature").TrackValue(36.6);
                // ---
            }

            {
                // ...
                (new TelemetryClient()).GetMetric("Temperature").TrackValue(39.1);
                // ---
            }

            // In this case the expected behavior is that these values are aggregated together into a single aggregate with Count = 2, Sum = 75.7 and so on.
            // In order to achive that, we use a single MetricManager to create all the respective metric series. This manager is attached to the
            // TelemetryConfiguration that stands behind a TelemetryClient. This ensures that the two (new TelemetryClient()).GetMetric("Temperature") statements
            // above return the same Metric object.
            // However, if different TelemetryClient instances return the name Metric instance, than what Context should the Metric respect?
            // To avoid confusion, it repsects none.

            // The best workaround for this circumstance was mentioned in a previous example - use the MetricSeries.AdditionalDataContext property.
            // However, sometimes it is inconvennient. For example, if you already created a cashed TelemetryClient for a specific scope and set some custom 
            // Context properties. 
            // It is actually possible to create a metric that is only scoped to a single TelemetryClient instance. This will cause the creation of a special
            // MetricManager instance at the scope of that one TelemetryClient. We highly recommend using this freature with restraint, as a MetricManager can
            // use a non-trivial ammount of resources, including separate aggregators for each metric series and a managed thread for sending aggregated telemetry.
            // Here how this works:

            TelemetryClient operationClient = new TelemetryClient();
            operationClient.Context.Operation.Name = "Operation XYZ";                       // This client will only send telemetry related to a specific operation.
            operationClient.InstrumentationKey = "05B5093A-F137-4A68-B826-A950CB68C68F";    // This cleint sends telemetry to a special Application Insights component.

            Metric operationRequestSize = operationClient.GetMetric("XYZ Request Size", MetricConfigurations.Measurement, MetricAggregationScope.TelemetryClient);

            int requestSize = GetCurrentRequestSize();
            operationRequestSize.TrackValue(306000);

            // Note the last parameter to GetMetric: MetricAggregationScope.TelemetryClient. This instructed the GetMetric API not to use the metric
            // manager at the TelemetryConfiguration scope, but to create and use a metric manager at the respecive cleint scope instead.
        }

        private static void RunSomeCode()
        {
            throw new ApplicationException();
        }

        private static int GetCurrentRequestSize()
        {
            // Do stuff
            return 11000;
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example06ab
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TraceSeveretyLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel;
    
    /// <summary>
    /// In this example we discuss how to write unit tests that validate that metrics are sent correctly
    /// We will consider two approaches:
    ///  a) Capturing all telemetry emitted by a method, including, but not limited to Metric Telemetry,
    ///     where a telemetry client can be injected.
    ///  b) Capturing all telemetry emitted by a method, including, but not limited to Metric Telemetry,
    ///     where the (new TelemetryClient()).TrackXxx(..) pattern in used in-line.
    ///  c) Capturing metric aggregates only.
    /// </summary>
    public class Sample06ab
    {
        /// <summary />
        public static void ExecA()
        {
            // *** UNIT TESTS: CAPTURING APLICATION INSIGHTS TELEMETRY BY INJECTING A TELEMETRY CLIENT ***

            // Here we will use a common unit test to capture and verify all Application Insights telemetry emitted by a
            // method SellPurpleDucks() of class ServiceClassA. We also assume that the class has been prepared for testing by allowing
            // to specify a telemetry client using dependency injection. The code for the class is listed below.

            // In a production application the class will probably be instantiated and called like this:

            {
                ServiceClassA serviceA = new ServiceClassA(new TelemetryClient());
                serviceA.SellPurpleDucks(42);
            }

            // In a unit test you will need to create a custom telemetry configuration that routs the emitted telemetry into a
            // datastructure for later inspection. There is a TestUtil class below that shows how to do that. Here is the unit test:

            {
                // Create the test pipeline and client:
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = TestUtil.CreateApplicationInsightsTelemetryConfiguration(out telemetrySentToChannel);
                using (telemetryPipeline)
                { 
                    TelemetryClient telemetryClient = new TelemetryClient(telemetryPipeline);

                    // Invoke method bein tested:
                    ServiceClassA serviceA = new ServiceClassA(telemetryClient);
                    serviceA.SellPurpleDucks(42);

                    // Make sure all telemetry is collected:
                    telemetryClient.Flush();

                    // Flushing the MetricManager is particularly important since the aggregation period of 1 minute has just started:
                    telemetryPipeline.Metrics().Flush();

                    // Verify that the right telemetry was sent:
                    Assert.AreEqual(2, telemetrySentToChannel.Count);

                    TraceTelemetry[] traceItems = telemetrySentToChannel.Where( (t) => ((t != null) && (t is TraceTelemetry)) )
                                                                        .Select( (t) => ((TraceTelemetry) t) )
                                                                        .ToArray();
                    Assert.AreEqual(1, traceItems.Length);
                    Assert.AreEqual("Stuff #1 completed", traceItems[0].Message);
                    Assert.AreEqual(TraceSeveretyLevel.Information, traceItems[0].SeverityLevel);

                    MetricTelemetry[] metricItems = telemetrySentToChannel.Where( (t) => ((t != null) && (t is MetricTelemetry)) )
                                                                          .Select( (t) => ((MetricTelemetry) t) )
                                                                          .ToArray();
                    Assert.AreEqual(1, metricItems.Length);
                    Assert.AreEqual("Ducks Sold", metricItems[0].Name);
                    Assert.AreEqual(1, metricItems[0].Count);
                    Assert.AreEqual(42, metricItems[0].Sum);
                    Assert.AreEqual(42, metricItems[0].Min);
                    Assert.AreEqual(42, metricItems[0].Max);
                    Assert.AreEqual(0, metricItems[0].StandardDeviation);
                    Assert.AreEqual(2, metricItems[0].Properties.Count);
                    Assert.IsTrue(metricItems[0].Properties.ContainsKey("_MS.AggregationIntervalMs"));
                    Assert.IsTrue(metricItems[0].Properties.ContainsKey("Color"));
                    Assert.AreEqual("Purple", metricItems[0].Properties["Color"]);

                    // Note that this test requires understanding how metric dimensions and other information such as aggregation period will
                    // be serielized into the Properties of the MetricTelemetry item. We will see how to avoid diving into these low level details
                    // when we see how to unit test by capturing the metric aggregates directly.
                }
            }
        }

        /// <summary />
        public static void ExecB()
        {
            // *** UNIT TESTS: CAPTURING APLICATION INSIGHTS TELEMETRY BY SUBSTITUTING THE TELEMETRY CHANNEL ***

            // Previously we used dependency injection to provide a custom telemetry client to test a method.

            // Consider now a slightly modified class ServiceClassB that does not expect a a custom telemetry client.
            // We can test it by substituting the channel used in the the default telemetry pipeline. 

            // In a production application the class will probably be instantiated and called like this:

            {
                ServiceClassB serviceB = new ServiceClassB();
                serviceB.SellPurpleDucks(42);
            }

            // Here is the unit test:

            {
                // Do not forget to set the InstrumentationKey to some value, otherwise the pipeline will not send any telemetry to the channel.
                TelemetryConfiguration.Active.InstrumentationKey = Guid.NewGuid().ToString("D");

                // Although this approach is more widely applicable, and does not require to prepare yor code for injection of the telemetry client,
                // in this model different unit tests can interfere with each other via the static default telemetry pipeline.
                // Such interference may be non-trivial. For this simple test, we need to flush out all the tracked values from the code that just run.
                // This will flush out all Measurements, but not counters, since they persist between flushes. This can unit testing with this method quite complex.
                // Otherwise they will interfere with the counts assertions below.
                TelemetryConfiguration.Active.Metrics().Flush();
                (new TelemetryClient(TelemetryConfiguration.Active)).Flush();

                // Create the test pipeline and client.
                StubTelemetryChannel telemetryCollector = new StubTelemetryChannel();
                TelemetryConfiguration.Active.TelemetryChannel = telemetryCollector;
                TelemetryConfiguration.Active.InstrumentationKey = Guid.NewGuid().ToString("D");

                // Invoke method bein tested:
                ServiceClassB serviceB = new ServiceClassB();
                serviceB.SellPurpleDucks(42);

                // Flushing the MetricManager is particularly important since the aggregation period of 1 minute has just started:
                TelemetryConfiguration.Active.Metrics().Flush();

                // As mentioned, tests using this approach interfere with each other.
                // For example, when running all the exaples after each other, counters from previous examples are still associated with the
                // metric manager at TelemetryConfiguration.Active.Metrics(). Luckily, all their names begin with "Items", so we can filter them out.

                ITelemetry[] telemetryFromThisTest = telemetryCollector.TelemetryItems
                                                                       .Where( (t) => !((t is MetricTelemetry) && ((MetricTelemetry) t).Name.StartsWith("Items")) )
                                                                       .ToArray();

                // Verify that the right telemetry was sent:

                Assert.AreEqual(2, telemetryFromThisTest.Length);

                TraceTelemetry[] traceItems = telemetryFromThisTest.Where( (t) => ((t != null) && (t is TraceTelemetry)) )
                                                                   .Select( (t) => ((TraceTelemetry) t) )
                                                                   .ToArray();
                Assert.AreEqual(1, traceItems.Length);
                Assert.AreEqual("Stuff #1 completed", traceItems[0].Message);
                Assert.AreEqual(TraceSeveretyLevel.Information, traceItems[0].SeverityLevel);

                MetricTelemetry[] metricItems = telemetryFromThisTest.Where( (t) => ((t != null) && (t is MetricTelemetry)) )
                                                                     .Select( (t) => ((MetricTelemetry) t) )
                                                                     .ToArray();
                Assert.AreEqual(1, metricItems.Length);
                Assert.AreEqual("Ducks Sold", metricItems[0].Name);
                Assert.AreEqual(1, metricItems[0].Count);
                Assert.AreEqual(42, metricItems[0].Sum);
                Assert.AreEqual(42, metricItems[0].Min);
                Assert.AreEqual(42, metricItems[0].Max);
                Assert.AreEqual(0, metricItems[0].StandardDeviation);
                Assert.AreEqual(2, metricItems[0].Properties.Count);
                Assert.IsTrue(metricItems[0].Properties.ContainsKey("_MS.AggregationIntervalMs"));
                Assert.IsTrue(metricItems[0].Properties.ContainsKey("Color"));
                Assert.AreEqual("Purple", metricItems[0].Properties["Color"]);

                // Note that this test requires understanding how metric dimensions and other information such as aggregation period will
                // be serielized into the Properties of the MetricTelemetry item. We will see how to avoid diving into these low level details
                // when we see how to unit test by capturing the metric aggregates directly.
            }
        }

        /// <summary />
        public static void ExecC()
        {
            // *** UNIT TESTS: CAPTURING METRIC AGGREGATES ***

            // Previously described test approaches intercept all application insights telemetry.
            // There are some drawbacks to such tests that are rare, but cab be significant in some circumstances:
            //  - Tests using channel substitution (model b) can interfere with each other (see above), making testing overly complex.
            //  - Since all telemetry is intercepted, such testing is suibale for unit tests only, but not for some integration or
            //    production test scenarios were telemetry needs to be actually sent to the cloud.
            //  - It is not applicable in the advanced scenarios where metric aggregates are sent to a consumer other that the
            //    Application Insights cloud endpoint.
            //  - It requires insights in how MetricAggregates are seriealized to MetricTelemetry items. Such seerialization may
            //    change over time if new metric aggregation kinds are supported by the processing backend.
            // Here, we use a custom aggregation cycle to bypass this limitation.

            // Consider now a slightly modified class ServiceClassB that does not expect a a custom telemetry client.
            // We can test it by substituting the channel used in the the default telemetry pipeline. 

            // In a production application the class will probably be instantiated and called like this:

            {
                ServiceClassB serviceB = new ServiceClassB();
                serviceB.SellPurpleDucks(42);
            }

            // Here is the unit test:

            {
                // Flush out all the tracked values from the code that just run. Otherwise they will interfere with the counts assertions below.
                TelemetryConfiguration.Active.Metrics().Flush();

                // Create the test pipeline and client.
                // Do not forget to set the InstrumentationKey to some value, otherwise the pipeline will not send any telemetry to the channel.
                StubTelemetryChannel telemetryCollector = new StubTelemetryChannel();
                TelemetryConfiguration.Active.TelemetryChannel = telemetryCollector;
                TelemetryConfiguration.Active.InstrumentationKey = Guid.NewGuid().ToString("D");

                // Invoke method bein tested:
                ServiceClassB serviceB = new ServiceClassB();
                serviceB.SellPurpleDucks(42);

                // Flushing the MetricManager is particularly important since the aggregation period of 1 minute has just started:
                TelemetryConfiguration.Active.Metrics().Flush();

                // Verify that the right telemetry was sent:
                Assert.AreEqual(2, telemetryCollector.TelemetryItems.Count);

                TraceTelemetry[] traceItems = telemetryCollector.TelemetryItems.Where( (t) => ((t != null) && (t is TraceTelemetry)) )
                                                                                 .Select( (t) => ((TraceTelemetry) t) )
                                                                                 .ToArray();
                Assert.AreEqual(1, traceItems.Length);
                Assert.AreEqual("Stuff #1 completed", traceItems[0].Message);
                Assert.AreEqual(TraceSeveretyLevel.Information, traceItems[0].SeverityLevel);

                MetricTelemetry[] metricItems = telemetryCollector.TelemetryItems.Where( (t) => ((t != null) && (t is MetricTelemetry)) )
                                                                                   .Select( (t) => ((MetricTelemetry) t) )
                                                                                   .ToArray();
                Assert.AreEqual(1, metricItems.Length);
                Assert.AreEqual("Ducks Sold", metricItems[0].Name);
                Assert.AreEqual(1, metricItems[0].Count);
                Assert.AreEqual(42, metricItems[0].Sum);
                Assert.AreEqual(42, metricItems[0].Min);
                Assert.AreEqual(42, metricItems[0].Max);
                Assert.AreEqual(0, metricItems[0].StandardDeviation);
                Assert.AreEqual(2, metricItems[0].Properties.Count);
                Assert.IsTrue(metricItems[0].Properties.ContainsKey("_MS.AggregationIntervalMs"));
                Assert.IsTrue(metricItems[0].Properties.ContainsKey("Color"));
                Assert.AreEqual("Purple", metricItems[0].Properties["Color"]);

                // Note that this test requires understanding how metric dimensions and other information such as aggregation period will
                // be serielized into the Properties of the MetricTelemetry item. We will see how to avoid diving into these low level details
                // when we see how to unit test by capturing the metric aggregates directly.
            }
        }
    }

    internal class ServiceClassA
    {
        private TelemetryClient _telemetryClient = null;

        public ServiceClassA(TelemetryClient telemetryClient)
        {
            if (telemetryClient == null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            _telemetryClient = telemetryClient;
        }

        public void SellPurpleDucks(int count)
        {
            // Do some stuff #1...
            _telemetryClient.TrackTrace("Stuff #1 completed", TraceSeveretyLevel.Information);

            // Do more stuff...
            _telemetryClient.GetMetric("Ducks Sold", "Color").TryTrackValue(count, "Purple");
        }
    }

    internal class ServiceClassB
    {
        public ServiceClassB()
        {
        }

        public void SellPurpleDucks(int count)
        {
            // Do some stuff #1...
            (new TelemetryClient()).TrackTrace("Stuff #1 completed", TraceSeveretyLevel.Information);

            // Do more stuff...
            (new TelemetryClient()).GetMetric("Ducks Sold", "Color").TryTrackValue(count, "Purple");
        }
    }

    internal class TestUtil
    {
        public static TelemetryConfiguration CreateApplicationInsightsTelemetryConfiguration(out IList<ITelemetry> telemetrySentToChannel)
        {
            StubTelemetryChannel channel = new StubTelemetryChannel();
            string iKey = Guid.NewGuid().ToString("D");
            TelemetryConfiguration telemetryConfig = new TelemetryConfiguration(iKey, channel);

            var channelBuilder = new TelemetryProcessorChainBuilder(telemetryConfig);
            channelBuilder.Build();

            foreach (ITelemetryProcessor initializer in telemetryConfig.TelemetryInitializers)
            {
                ITelemetryModule m = initializer as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            foreach (ITelemetryProcessor processor in telemetryConfig.TelemetryProcessors)
            {
                ITelemetryModule m = processor as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            telemetrySentToChannel = channel.TelemetryItems;
            return telemetryConfig;
        }
    }

    internal class StubTelemetryChannel : ITelemetryChannel
    {
        public StubTelemetryChannel()
        {
            TelemetryItems = new List<ITelemetry>();
        }

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress { get; set; }

        public IList<ITelemetry> TelemetryItems { get; }

        public void Send(ITelemetry item)
        {
            TelemetryItems.Add(item);
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }
    }
}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace User.Namespace.Example06c
{
    using System;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TraceSeveretyLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel;
    
    /// <summary>
    /// In this example we discuss how to write unit tests that validate that metrics are sent correctly
    /// We will consider an advanced unit test approach:
    ///  c) Capturing metric aggregates only.
    /// </summary>
    public class Sample06c
    {
        /// <summary />
        public static void ExecC()
        {
            // *** UNIT TESTS: CAPTURING METRIC AGGREGATES ***

            // Previously described test approaches intercept all application insights telemetry.
            // There are some drawbacks to such tests that are rare, but cab be significant in some circumstances:
            //  - Since all telemetry is intercepted, such testing is suibale for unit tests only, but not for some integration or
            //    production test scenarios were telemetry needs to be actually sent to the cloud.
            //  - It is not applicable in the advanced scenarios where metric aggregates are sent to a consumer other that the
            //    Application Insights cloud endpoint.
            //  - It requires insights in how MetricAggregates are seriealized to MetricTelemetry items. Such seerialization may
            //    change over time if new metric aggregation kinds are supported by the processing backend.
            // Here, we use a custom aggregation cycle to bypass these limitations.

            // Previousy we mentioned that a MetricManager encapsulates a managed thread that drives the default aggregation cycle and sends
            // metric aggregates to the cloud every minute.
            // In fact, there are 3 aggregation cycles. Beyond the default cycle, there is a custom cycle and an additional cycle dedicated
            // specifically for QuickPulse/LiveMetrics integration. Users should not me using the QuickPulse cycle for their code.
            // Here we discuss the custom aggregation cycle.
            // Aggregation cycles other than Default do not add additional threads.
            // Instead, they track values into additional aggregators and allow users to pull data when desired. Thus, user have full controll
            // over timing issues.

            // In the context of testing, users can use "virtual time", i.e. they can specify any timestamps in a test that
            // runs only for milliseconds, thus testing varous timing scenarios.

            DateTimeOffset testStartTime = new DateTimeOffset(2017, 11, 1, 13, 0, 0, TimeSpan.FromHours(8));

            // In order to use custom aggregation cycles and other advanced metrics features, import the following namespace:
            // using Microsoft.ApplicationInsights.Metrics.Extensibility;

            // By default all non-default aggregation cycles are inactive. To activate the custom cycle, request the custom cycle aggregates:

            MetricManager defaultMetricManager = TelemetryConfiguration.Active.Metrics();
            AggregationPeriodSummary lastCycle = defaultMetricManager.StartOrCycleAggregators(
                                                                                MetricAggregationCycleKind.Custom,
                                                                                testStartTime,
                                                                                ExcludeCountersFromPreviousTestsFilter.Instance);

            // If the cycle was inactive so far, it will be started up and aggregation into the cycle will begin. Other cycles will be unaffected.
            // Since this was the first invocation, the received AggregationPeriodSummary is empty:

            Assert.AreEqual(0, lastCycle.NonpersistentAggregates.Count);

            // Now we can call the methos being tested.
            
            ServiceClassC serviceC = new ServiceClassC();
            serviceC.SellPurpleDucks(42);

            // Now we can pull the data again. Let us pretend that 1 full minute has passed:

            lastCycle = defaultMetricManager.StartOrCycleAggregators(
                                                    MetricAggregationCycleKind.Custom,
                                                    testStartTime.AddMinutes(1),
                                                    ExcludeCountersFromPreviousTestsFilter.Instance);

            // Now we can verify that metrics were tracked correctly:

            Assert.AreEqual(1, lastCycle.NonpersistentAggregates.Count, "One Measurement should be tracked");
            Assert.AreEqual(0, lastCycle.PersistentAggregates.Count, "No Counters should be tracked");

            Assert.AreEqual("Ducks Sold", lastCycle.NonpersistentAggregates[0].MetricId);
            Assert.AreEqual(MetricAggregateKinds.SimpleStatistics.Moniker, lastCycle.NonpersistentAggregates[0].AggregationKindMoniker);
            Assert.AreEqual(testStartTime, lastCycle.NonpersistentAggregates[0].AggregationPeriodStart);
            Assert.AreEqual(TimeSpan.FromMinutes(1), lastCycle.NonpersistentAggregates[0].AggregationPeriodDuration);
            Assert.AreEqual(1, lastCycle.NonpersistentAggregates[0].Dimensions.Count);
            Assert.AreEqual("Purple", lastCycle.NonpersistentAggregates[0].Dimensions["Color"]);
            Assert.AreEqual(1, lastCycle.NonpersistentAggregates[0].GetAggregateData<int>(MetricAggregateKinds.SimpleStatistics.DataKeys.Count, -1));
            Assert.AreEqual(42, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Sum, -1));
            Assert.AreEqual(42, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Min, -1));
            Assert.AreEqual(42, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Max, -1));
            Assert.AreEqual(0, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev, -1));

            // Note that becasue "Ducks Sold" is a Measurement, the and we cycled the cycle aggregators, the current aggregator is not empty.
            // However, if it was a counter, it would keep the values tracked thus far. To help differentiate between thes two cases, Measurement-like
            // aggregates are contained within AggregationPeriodSummary.NonpersistentAggregates and  Counter-like aggregates are contained within
            // AggregationPeriodSummary.PersistentAggregates.

            // Let's call the tested API again, now twice:

            serviceC.SellPurpleDucks(11);
            serviceC.SellPurpleDucks(12);

            // Since we are now done, we will gracefully shut down the custom aggregation cycle. We will receive the last aggregates:

            lastCycle = defaultMetricManager.StopAggregators(MetricAggregationCycleKind.Custom, testStartTime.AddMinutes(2));

            Assert.AreEqual(1, lastCycle.NonpersistentAggregates.Count, "One Measurement should be tracked (with two values)");
            Assert.AreEqual(0, lastCycle.PersistentAggregates.Count, "No Counters should be tracked");

            Assert.AreEqual("Ducks Sold", lastCycle.NonpersistentAggregates[0].MetricId);
            Assert.AreEqual(MetricAggregateKinds.SimpleStatistics.Moniker, lastCycle.NonpersistentAggregates[0].AggregationKindMoniker);
            Assert.AreEqual(testStartTime.AddMinutes(1), lastCycle.NonpersistentAggregates[0].AggregationPeriodStart);
            Assert.AreEqual(TimeSpan.FromMinutes(1), lastCycle.NonpersistentAggregates[0].AggregationPeriodDuration);
            Assert.AreEqual(1, lastCycle.NonpersistentAggregates[0].Dimensions.Count);
            Assert.AreEqual("Purple", lastCycle.NonpersistentAggregates[0].Dimensions["Color"]);
            Assert.AreEqual(2, lastCycle.NonpersistentAggregates[0].GetAggregateData<int>(MetricAggregateKinds.SimpleStatistics.DataKeys.Count, -1));
            Assert.AreEqual(23, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Sum, -1));
            Assert.AreEqual(11, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Min, -1));
            Assert.AreEqual(12, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.Max, -1));
            Assert.AreEqual(0.5, lastCycle.NonpersistentAggregates[0].GetAggregateData<double>(MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev, -1));
        }
    }

    internal class ServiceClassC
    {
        public ServiceClassC()
        {
        }

        public void SellPurpleDucks(int count)
        {
            // Do some stuff #1...
            (new TelemetryClient()).TrackTrace("Stuff #1 completed", TraceSeveretyLevel.Information);

            // Do more stuff...
            (new TelemetryClient()).GetMetric("Ducks Sold", "Color").TryTrackValue(count, "Purple");
        }
    }

    internal class ExcludeCountersFromPreviousTestsFilter : IMetricSeriesFilter
    {
        public static readonly ExcludeCountersFromPreviousTestsFilter Instance = new ExcludeCountersFromPreviousTestsFilter();

        public bool WillConsume(MetricSeries dataSeries, out IMetricValueFilter valueFilter)
        {
            valueFilter = null;
            return !dataSeries.MetricId.StartsWith("Items");
        }
    }

}
// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------
namespace Microsoft.ApplicationInsights.Metrics.Examples
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This class runs all examples.
    /// </summary>
    [TestClass]
    public class RunAllExamples
    {
        /// <summary />
        [TestMethod]
        public void Example01()
        {
            User.Namespace.Example01.Sample01.Exec();
        }

        /// <summary />
        [TestMethod]
        public void Example02()
        {
            User.Namespace.Example02.Sample02.Exec();
        }

        /// <summary />
        [TestMethod]
        public void Example03()
        {
            User.Namespace.Example03.Sample03.Exec();
        }

        /// <summary />
        [TestMethod]
        public void Example04()
        {
            User.Namespace.Example04.Sample04.Exec();
        }

        /// <summary />
        [TestMethod]
        public void Example05()
        {
            User.Namespace.Example05.Sample05.Exec();
        }

        /// <summary />
        [TestMethod]
        public void Example06()
        {
            //User.Namespace.Example06ab.Sample06ab.ExecA();
            User.Namespace.Example06ab.Sample06ab.ExecB();
            //User.Namespace.Example06c.Sample06c.ExecC();
        }
    }
}