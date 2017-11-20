using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

using Microsoft.ApplicationInsights.Metrics.TestUtil;
using System.Globalization;
using System.Text;

using CycleKind = Microsoft.ApplicationInsights.Metrics.Extensibility.MetricAggregationCycleKind;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class NaiveDistinctCountMetricSeriesAggregatorTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new NaiveDistinctCountMetricSeriesAggregator(configuration: null, dataSeries: null, aggregationCycleKind: CycleKind.Custom));

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);
                Assert.IsNotNull(aggregator);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);
                Assert.IsNotNull(aggregator);
            }
        }

        /// <summary />
        [TestMethod]
        public void CaseSensitivity()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodMillis = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false, caseSensitiveDistinctions: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                aggregator.TrackValue("Foo");
                aggregator.TrackValue("Bar");
                aggregator.TrackValue("FOO");
                aggregator.TrackValue("bar");
                aggregator.TrackValue("Foo");

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false, caseSensitiveDistinctions: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                aggregator.TrackValue("Foo");
                aggregator.TrackValue("Bar");
                aggregator.TrackValue("FOO");
                aggregator.TrackValue("bar");
                aggregator.TrackValue("Foo");

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                aggregator.TrackValue("Foo");
                aggregator.TrackValue("Bar");
                aggregator.TrackValue("FOO");
                aggregator.TrackValue("bar");
                aggregator.TrackValue("Foo");

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueDouble()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodMillis = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;

            var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            aggregator.TrackValue(-42);

            MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(58 - 100);
            aggregator.TrackValue("-42");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(-42.0);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(-42.0000);
            aggregator.TrackValue(58.0 - 100);
            aggregator.TrackValue(58.5 - 100.5);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue("-42.0");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(42);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 3, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue("42");
            aggregator.TrackValue("   42 \t");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(Double.NaN);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue((object) Double.NaN);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 12, sum: 5, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(Double.PositiveInfinity);
            aggregator.TrackValue(Double.NegativeInfinity);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 7, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodMillis = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(null);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue("null");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue("Foo");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(" Foo\n");
                aggregator.TrackValue(" \t Foo\n\r");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 4, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "Foo" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 4, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "BAR" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 5, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue('\n' + "BAR" + "   ");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 6, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 7, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => String.Empty ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 7, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "    " ));
                aggregator.TrackValue(new Stringer( () => "\t" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: 9, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "\0" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 12, sum: 10, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false, caseSensitiveDistinctions: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                for (int i = 0; i < 100000; i++)
                {
                    StringBuilder s = new StringBuilder();
                    for (int j = 0; j < 100; j++)
                    {
                        if (j % 2 == 0)
                        {
                            s.Append('X');
                        }
                        else
                        {
                            s.Remove(s.Length - 1, 1);
                            s.Append('x');
                        }
                        
                        aggregator.TrackValue(s.ToString());
                    }
                }

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10000000, sum: 100, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false, caseSensitiveDistinctions: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                for (int i = 0; i < 100000; i++)
                {
                    StringBuilder s = new StringBuilder();
                    for (int j = 0; j < 100; j++)
                    {
                        if (j % 2 == 0)
                        {
                            s.Append('X');
                        }
                        else
                        {
                            s.Remove(s.Length - 1, 1);
                            s.Append('x');
                        }

                        aggregator.TrackValue(s.ToString());
                    }
                }

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10000000, sum: 50, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
            }
        }

        private static void ValidateNumericAggregateValues(MetricAggregate aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, long periodMs)
        {
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name, count, sum, max, min, stdDev, timestamp, periodMs, "Microsoft.Azure.NaiveDistinctCount");
        }

        /// <summary />
        [TestMethod]
        public void CreateAggregateUnsafe()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false);
            var metric = new MetricSeries(
                                aggregationManager,
                                "Distinct Cows Sold",
                                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Dim 1", "DV1"),
                                                                     new KeyValuePair<string, string>("Dim 2", "DV2"),
                                                                     new KeyValuePair<string, string>("Dim 3", "DV3"),
                                                                     new KeyValuePair<string, string>("Dim 2", "DV2a")},
                                seriesConfig);

            var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (MetricSeriesConfigurationForNaiveDistinctCount) metric.GetConfiguration(),
                                                    metric,
                                                    CycleKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            aggregator.Reset(startTS, valueFilter: null);

            aggregator.TrackValue("Foo");
            aggregator.TrackValue("Bar");
            aggregator.TrackValue("Foo");

            MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
            Assert.IsNotNull(aggregate);

            Assert.AreEqual("Distinct Cows Sold", aggregate.MetricId, "aggregate.MetricId mismatch");
            Assert.AreEqual(3, aggregate.AggregateData["TotalCount"], "aggregate.AggregateData[TotalCount] mismatch");
            Assert.AreEqual(2, aggregate.AggregateData["DistinctCount"], "aggregate.AggregateData[DistinctCount] mismatch");

            Assert.AreEqual(startTS, aggregate.AggregationPeriodStart, "metricAggregate.Timestamp mismatch");
            Assert.AreEqual(
                        (endTS - startTS).TotalMilliseconds,
                        aggregate.AggregationPeriodDuration.TotalMilliseconds,
                        "aggregate.AggregationPeriodDuration mismatch");

            Assert.AreEqual(3, aggregate.Dimensions.Count);

            Assert.IsTrue(aggregate.Dimensions.ContainsKey("Dim 1"));
            Assert.AreEqual("DV1", aggregate.Dimensions["Dim 1"]);

            Assert.IsTrue(aggregate.Dimensions.ContainsKey("Dim 2"));
            Assert.AreEqual("DV2a", aggregate.Dimensions["Dim 2"]);

            Assert.IsTrue(aggregate.Dimensions.ContainsKey("Dim 3"));
            Assert.AreEqual("DV3", aggregate.Dimensions["Dim 3"]);

        }

        /// <summary />
        [TestMethod]
        public void TryRecycle()
        {
            var nonpersistentAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            var persistentAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            long periodMillisDef = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;
            long periodMillisStart = (long) (endTS - startTS).TotalMilliseconds;


            {
                nonpersistentAggregator.Reset(startTS, valueFilter: null);

                nonpersistentAggregator.TrackValue(10);
                nonpersistentAggregator.TrackValue(20);
                nonpersistentAggregator.TrackValue(10);

                MetricAggregate aggregate = nonpersistentAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);

                bool canRecycle = nonpersistentAggregator.TryRecycle();
                Assert.IsTrue(canRecycle);

                aggregate = nonpersistentAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillisDef);

                canRecycle = nonpersistentAggregator.TryRecycle();
                Assert.IsTrue(canRecycle);

                aggregate = nonpersistentAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillisDef);
            }
            {
                persistentAggregator.Reset(startTS, valueFilter: null);

                persistentAggregator.TrackValue(-10);
                persistentAggregator.TrackValue(-20);
                persistentAggregator.TrackValue(-10);

                MetricAggregate aggregate = persistentAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);

                bool canRecycle = persistentAggregator.TryRecycle();
                Assert.IsFalse(canRecycle);

                aggregate = persistentAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);
            }
        }

        /// <summary />
        [TestMethod]
        public void GetDataSeries()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", null, seriesConfig);

            var aggregatorForConcreteSeries = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (MetricSeriesConfigurationForNaiveDistinctCount) metric.GetConfiguration(),
                                                    dataSeries: metric,
                                                    aggregationCycleKind: CycleKind.Custom);

            var aggregatorForNullSeries = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

            Assert.IsNotNull(aggregatorForConcreteSeries.DataSeries);
            Assert.AreSame(metric, aggregatorForConcreteSeries.DataSeries);

            Assert.IsNull(aggregatorForNullSeries.DataSeries);
        }

        /// <summary />
        [TestMethod]
        public void Reset()
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            long periodMillisDef = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;
            long periodMillisStart = (long) (endTS - startTS).TotalMilliseconds;

            {
                // Measurement:
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                int filterDoubleInvocationsCount = 0;
                int filterObjectInvocationsCount = 0;

                aggregator.TrackValue("Cow 1");
                aggregator.TrackValue("Cow 2");
                aggregator.TrackValue("Cow 2");

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillisDef);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.Reset(
                               startTS,
                               new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                               filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                               filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; }));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 4");
                aggregator.TrackValue("Cow 4");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(4, filterObjectInvocationsCount);
            }
            {
                // Accumulator:
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                int filterDoubleInvocationsCount = 0;
                int filterObjectInvocationsCount = 0;

                aggregator.TrackValue("Cow 1");
                aggregator.TrackValue("Cow 2");
                aggregator.TrackValue("Cow 2");

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillisDef);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.Reset(
                               startTS,
                               new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                               filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                               filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; }));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 4");
                aggregator.TrackValue("Cow 4");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(4, filterObjectInvocationsCount);
            }

        }

        /// <summary />
        [TestMethod]
        public void CompleteAggregation()
        {
            var aggregationManager = new MetricAggregationManager();

            var nonPersistentConfig = new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: false);
            var nonPersistentMetric = new MetricSeries(aggregationManager, "Unique Cows Sold", null, nonPersistentConfig);

            var nonPersistentAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (MetricSeriesConfigurationForNaiveDistinctCount) nonPersistentMetric.GetConfiguration(),
                                                    nonPersistentMetric,
                                                    CycleKind.Custom);

            var persistentConfig = new MetricSeriesConfigurationForNaiveDistinctCount(usePersistentAggregation: true);
            var persistentMetric = new MetricSeries(aggregationManager, "Unique Cows Sold", null, persistentConfig);

            var persistentAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (MetricSeriesConfigurationForNaiveDistinctCount) persistentMetric.GetConfiguration(),
                                                    persistentMetric,
                                                    CycleKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodMillis = (long) (endTS - startTS).TotalMilliseconds;

            int filterDoubleInvocationsCount = 0;
            int filterObjectInvocationsCount = 0;

            nonPersistentAggregator.Reset(
                                startTS,
                                new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                                filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                                filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; } ));

            Assert.AreEqual(0, filterDoubleInvocationsCount);
            Assert.AreEqual(0, filterObjectInvocationsCount);

            nonPersistentAggregator.TrackValue(1);
            nonPersistentAggregator.TrackValue("2");
            nonPersistentAggregator.TrackValue(2);

            MetricAggregate aggregate = nonPersistentAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(2, filterDoubleInvocationsCount);
            Assert.AreEqual(1, filterObjectInvocationsCount);

            nonPersistentAggregator.TrackValue("3");
            nonPersistentAggregator.TrackValue(4);
            
            aggregate = nonPersistentAggregator.CompleteAggregation(endTS);

            //// We had this originally when completed agregators did not take any more values when they were non-persistent. This complexity has no benefit.
            //ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            //Assert.AreEqual(2, filterDoubleInvocationsCount);
            //Assert.AreEqual(1, filterObjectInvocationsCount);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(3, filterDoubleInvocationsCount);
            Assert.AreEqual(2, filterObjectInvocationsCount);

            filterDoubleInvocationsCount = 0;
            filterObjectInvocationsCount = 0;

            persistentAggregator.Reset(
                               startTS,
                               new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                               filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                               filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; }));

            Assert.AreEqual(0, filterDoubleInvocationsCount);
            Assert.AreEqual(0, filterObjectInvocationsCount);

            persistentAggregator.TrackValue(1);
            persistentAggregator.TrackValue("2");
            persistentAggregator.TrackValue("1");

            aggregate = persistentAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(1, filterDoubleInvocationsCount);
            Assert.AreEqual(2, filterObjectInvocationsCount);

            persistentAggregator.TrackValue("3");
            persistentAggregator.TrackValue(4);

            aggregate = persistentAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(2, filterDoubleInvocationsCount);
            Assert.AreEqual(3, filterObjectInvocationsCount);
        }

        private class Stringer
        {
            private readonly Func<string> _stringerFunction;

            public Stringer(Func<string> stringerFunction)
            {
                _stringerFunction = stringerFunction;
            }

            public override string ToString()
            {
                if (_stringerFunction == null)
                {
                    return base.ToString();
                }
                else
                {
                    return _stringerFunction();
                }
            }
        }
    }
}
