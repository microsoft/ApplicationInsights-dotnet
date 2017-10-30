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
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);
                Assert.IsNotNull(aggregator);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true),
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
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: false),
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
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: true),
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
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
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
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
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
            ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: 3, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            aggregator.TrackValue(Double.NaN);
            aggregator.TrackValue(Double.PositiveInfinity);
            aggregator.TrackValue(Double.NegativeInfinity);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 6, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillis);
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodMillis = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(null);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue("null");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 1, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue("Foo");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(" Foo\n");
                aggregator.TrackValue(" \t Foo\n\r");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "Foo" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "BAR" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 3, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue('\n' + "BAR" + "   ");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 3, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 4, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => String.Empty ));
                aggregator.TrackValue(new Stringer( () => "    " ));
                aggregator.TrackValue(new Stringer( () => "\t" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 12, sum: 4, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

                aggregator.TrackValue(new Stringer( () => "\0" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 13, sum: 5, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodMillis);

            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: true),
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
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: false),
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
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name, count, sum, max, min, stdDev, timestamp, periodMs);
        }

        /// <summary />
        [TestMethod]
        public void CreateAggregateUnsafe()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false);
            var metric = new MetricSeries(
                                aggregationManager,
                                "Distinct Cows Sold",
                                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Dim 1", "DV1"),
                                                                     new KeyValuePair<string, string>("Dim 2", "DV2"),
                                                                     new KeyValuePair<string, string>("Dim 3", "DV3"),
                                                                     new KeyValuePair<string, string>("Dim 2", "DV2a")},
                                seriesConfig);

            var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) metric.GetConfiguration(),
                                                    metric,
                                                    CycleKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            aggregator.Reset(startTS, valueFilter: null);

            metric.AdditionalDataContext.Component.Version = "C";
            metric.AdditionalDataContext.Device.Id = "D";
            metric.AdditionalDataContext.InstrumentationKey = "L";
            metric.AdditionalDataContext.Location.Ip = "M";
            metric.AdditionalDataContext.Operation.Id = "N";
            metric.AdditionalDataContext.Session.Id = "R";
            metric.AdditionalDataContext.User.AccountId = "S";
            metric.AdditionalDataContext.Properties["Prop 1"] = "W";
            metric.AdditionalDataContext.Properties["Prop 2"] = "X";
            metric.AdditionalDataContext.Properties["Dim 1"] = "Y";

            aggregator.TrackValue("Foo");
            aggregator.TrackValue("Bar");
            aggregator.TrackValue("Foo");

            MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
            Assert.IsNotNull(aggregate);

            Assert.AreEqual("Distinct Cows Sold", aggregate.MetricId, "aggregate.MetricId mismatch");
            Assert.AreEqual(3, aggregate.AggregateData["Count"], "aggregate.AggregateData[Count] mismatch");
            Assert.AreEqual(2, (double) aggregate.AggregateData["Sum"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[Sum] mismatch");
            Assert.AreEqual(0, (double) aggregate.AggregateData["Max"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[Max] mismatch");
            Assert.AreEqual(0, (double) aggregate.AggregateData["Min"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[Min] mismatch");
            Assert.AreEqual(0, (double) aggregate.AggregateData["StdDev"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[StdDev] mismatch");

            Assert.AreEqual(startTS, aggregate.AggregationPeriodStart, "metricAggregate.Timestamp mismatch");
            Assert.AreEqual(
                        (endTS - startTS).TotalMilliseconds,
                        aggregate.AggregationPeriodDuration.TotalMilliseconds,
                        "aggregate.AggregationPeriodDuration mismatch");

            Assert.IsNotNull(aggregate.AdditionalDataContext);
            Assert.IsInstanceOfType(aggregate.AdditionalDataContext, typeof(TelemetryContext));

            Assert.AreEqual("C", ((TelemetryContext) aggregate.AdditionalDataContext).Component.Version);
            Assert.AreEqual("D", ((TelemetryContext) aggregate.AdditionalDataContext).Device.Id);
            Assert.AreEqual("L", ((TelemetryContext) aggregate.AdditionalDataContext).InstrumentationKey);
            Assert.AreEqual("M", ((TelemetryContext) aggregate.AdditionalDataContext).Location.Ip);
            Assert.AreEqual("N", ((TelemetryContext) aggregate.AdditionalDataContext).Operation.Id);
            Assert.AreEqual("R", ((TelemetryContext) aggregate.AdditionalDataContext).Session.Id);
            Assert.AreEqual("S", ((TelemetryContext) aggregate.AdditionalDataContext).User.AccountId);

            Assert.AreEqual(3, ((TelemetryContext) aggregate.AdditionalDataContext).Properties.Count);

            Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey("Prop 1"));
            Assert.AreEqual("W", ((TelemetryContext) aggregate.AdditionalDataContext).Properties["Prop 1"]);

            Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey("Prop 2"));
            Assert.AreEqual("X", ((TelemetryContext) aggregate.AdditionalDataContext).Properties["Prop 2"]);

            Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey("Dim 1"));
            Assert.AreEqual("Y", ((TelemetryContext) aggregate.AdditionalDataContext).Properties["Dim 1"]);

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
            var measurementAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            var counterAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            long periodMillisDef = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;
            long periodMillisStart = (long) (endTS - startTS).TotalMilliseconds;


            {
                measurementAggregator.Reset(startTS, valueFilter: null);

                measurementAggregator.TrackValue(10);
                measurementAggregator.TrackValue(20);
                measurementAggregator.TrackValue(10);

                MetricAggregate aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);

                bool canRecycle = measurementAggregator.TryRecycle();
                Assert.IsTrue(canRecycle);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillisDef);

                canRecycle = measurementAggregator.TryRecycle();
                Assert.IsTrue(canRecycle);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodMillisDef);
            }
            {
                counterAggregator.Reset(startTS, valueFilter: null);

                counterAggregator.TrackValue(-10);
                counterAggregator.TrackValue(-20);
                counterAggregator.TrackValue(-10);

                MetricAggregate aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);

                bool canRecycle = counterAggregator.TryRecycle();
                Assert.IsFalse(canRecycle);

                aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillisStart);
            }
        }

        /// <summary />
        [TestMethod]
        public void GetDataSeries()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", null, seriesConfig);

            var aggregatorForConcreteSeries = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) metric.GetConfiguration(),
                                                    dataSeries: metric,
                                                    aggregationCycleKind: CycleKind.Custom);

            var aggregatorForNullSeries = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
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
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
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
                // Counter:
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true),
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

            var mesurementConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false);
            var measurementMetric = new MetricSeries(aggregationManager, "Unique Cows Sold", null, mesurementConfig);

            var measurementAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) measurementMetric.GetConfiguration(),
                                                    measurementMetric,
                                                    CycleKind.Custom);

            var counterConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true);
            var counterMetric = new MetricSeries(aggregationManager, "Unique Cows Sold", null, counterConfig);

            var counterAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) counterMetric.GetConfiguration(),
                                                    counterMetric,
                                                    CycleKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodMillis = (long) (endTS - startTS).TotalMilliseconds;

            int filterDoubleInvocationsCount = 0;
            int filterObjectInvocationsCount = 0;

            measurementAggregator.Reset(
                                startTS,
                                new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                                filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                                filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; } ));

            Assert.AreEqual(0, filterDoubleInvocationsCount);
            Assert.AreEqual(0, filterObjectInvocationsCount);

            measurementAggregator.TrackValue(1);
            measurementAggregator.TrackValue("2");
            measurementAggregator.TrackValue(2);

            MetricAggregate aggregate = measurementAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(2, filterDoubleInvocationsCount);
            Assert.AreEqual(1, filterObjectInvocationsCount);

            measurementAggregator.TrackValue("3");
            measurementAggregator.TrackValue(4);

            aggregate = measurementAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(2, filterDoubleInvocationsCount);
            Assert.AreEqual(1, filterObjectInvocationsCount);

            filterDoubleInvocationsCount = 0;
            filterObjectInvocationsCount = 0;

            counterAggregator.Reset(
                               startTS,
                               new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                               filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                               filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; }));

            Assert.AreEqual(0, filterDoubleInvocationsCount);
            Assert.AreEqual(0, filterObjectInvocationsCount);

            counterAggregator.TrackValue(1);
            counterAggregator.TrackValue("2");
            counterAggregator.TrackValue("1");

            aggregate = counterAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodMillis);
            Assert.AreEqual(1, filterDoubleInvocationsCount);
            Assert.AreEqual(2, filterObjectInvocationsCount);

            counterAggregator.TrackValue("3");
            counterAggregator.TrackValue(4);

            aggregate = counterAggregator.CompleteAggregation(endTS);
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
