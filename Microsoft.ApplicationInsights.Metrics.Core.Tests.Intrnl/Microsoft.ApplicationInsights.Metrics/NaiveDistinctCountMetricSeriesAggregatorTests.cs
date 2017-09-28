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
            Assert.ThrowsException<ArgumentNullException>(() => new NaiveDistinctCountMetricSeriesAggregator(configuration: null, dataSeries: null, consumerKind: MetricConsumerKind.Custom));

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);
                Assert.IsNotNull(aggregator);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);
                Assert.IsNotNull(aggregator);
            }
        }

        /// <summary />
        [TestMethod]
        public void CaseSensitivity()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue("Foo");
                aggregator.TrackValue("Bar");
                aggregator.TrackValue("FOO");
                aggregator.TrackValue("bar");
                aggregator.TrackValue("Foo");

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: true),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue("Foo");
                aggregator.TrackValue("Bar");
                aggregator.TrackValue("FOO");
                aggregator.TrackValue("bar");
                aggregator.TrackValue("Foo");

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue("Foo");
                aggregator.TrackValue("Bar");
                aggregator.TrackValue("FOO");
                aggregator.TrackValue("bar");
                aggregator.TrackValue("Foo");

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueDouble()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            aggregator.TrackValue(-42);

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue(58 - 100);
            aggregator.TrackValue("-42");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue(-42.0);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue(-42.0000);
            aggregator.TrackValue(58.0 - 100);
            aggregator.TrackValue(58.5 - 100.5);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 1, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("-42.0");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue(42);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 3, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("42");
            aggregator.TrackValue("   42 \t");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: 3, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue(Double.NaN);
            aggregator.TrackValue(Double.PositiveInfinity);
            aggregator.TrackValue(Double.NegativeInfinity);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 6, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(null);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue("null");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 1, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue("Foo");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(" Foo\n");
                aggregator.TrackValue(" \t Foo\n\r");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(new Stringer( () => "Foo" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 2, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(new Stringer( () => "BAR" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 3, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue('\n' + "BAR" + "   ");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 3, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(new Stringer( () => "" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 4, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(new Stringer( () => String.Empty ));
                aggregator.TrackValue(new Stringer( () => "    " ));
                aggregator.TrackValue(new Stringer( () => "\t" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 12, sum: 4, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(new Stringer( () => "\0" ));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 13, sum: 5, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: true),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

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
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10000000, sum: 100, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false, caseSensitiveDistinctions: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

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
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10000000, sum: 50, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
        }

        private static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, string periodMs)
        {
            CommonSimpleDataSeriesAggregatorTests.ValidateNumericAggregateValues(aggregate, name, count, sum, max, min, stdDev, timestamp, periodMs);
        }

        /// <summary />
        [TestMethod]
        public void CreateAggregateUnsafe()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false);
            var metric = new MetricSeries(aggregationManager, "Distinct Cows Sold", seriesConfig);

            var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) metric.GetConfiguration(),
                                                    metric,
                                                    MetricConsumerKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            aggregator.Reset(startTS, valueFilter: null);

            metric.Context.Component.Version = "C";
            metric.Context.Device.Id = "D";
            metric.Context.InstrumentationKey = "L";
            metric.Context.Location.Ip = "M";
            metric.Context.Operation.Id = "N";
            metric.Context.Session.Id = "R";
            metric.Context.User.AccountId = "S";
            metric.Context.Properties["Dim 1"] = "W";
            metric.Context.Properties["Dim 2"] = "X";
            metric.Context.Properties["Dim 3"] = "Y";

            aggregator.TrackValue("Foo");
            aggregator.TrackValue("Bar");
            aggregator.TrackValue("Foo");

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            Assert.IsNotNull(aggregate);

            MetricTelemetry metricAggregate = aggregate as MetricTelemetry;
            Assert.IsNotNull(metricAggregate);

            Assert.AreEqual("Distinct Cows Sold", metricAggregate.Name, "metricAggregate.Name mismatch");
            Assert.AreEqual(3, metricAggregate.Count, "metricAggregate.Count mismatch");
            Assert.AreEqual(2, metricAggregate.Sum, Utils.MaxAllowedPrecisionError, "metricAggregate.Sum mismatch");
            Assert.AreEqual(0, metricAggregate.Max.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Max mismatch");
            Assert.AreEqual(0, metricAggregate.Min.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Min mismatch");
            Assert.AreEqual(0, metricAggregate.StandardDeviation.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.StandardDeviation mismatch");

            Assert.AreEqual(startTS, metricAggregate.Timestamp, "metricAggregate.Timestamp mismatch");
            Assert.AreEqual(
                        ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture),
                        metricAggregate?.Properties?[Utils.AggregationIntervalMonikerPropertyKey],
                        "metricAggregate.Properties[AggregationIntervalMonikerPropertyKey] mismatch");

            Assert.AreEqual("C", metricAggregate.Context.Component.Version);
            Assert.AreEqual("D", metricAggregate.Context.Device.Id);
            Assert.AreEqual(String.Empty, metricAggregate.Context.InstrumentationKey);
            Assert.AreEqual("M", metricAggregate.Context.Location.Ip);
            Assert.AreEqual("N", metricAggregate.Context.Operation.Id);
            Assert.AreEqual("R", metricAggregate.Context.Session.Id);
            Assert.AreEqual("S", metricAggregate.Context.User.AccountId);

            Assert.IsTrue(metricAggregate.Context.Properties.ContainsKey("Dim 1"));
            Assert.AreEqual("W", metricAggregate.Context.Properties["Dim 1"]);

            Assert.IsTrue(metricAggregate.Context.Properties.ContainsKey("Dim 2"));
            Assert.AreEqual("X", metricAggregate.Context.Properties["Dim 2"]);

            Assert.IsTrue(metricAggregate.Context.Properties.ContainsKey("Dim 3"));
            Assert.AreEqual("Y", metricAggregate.Context.Properties["Dim 3"]);

            // ToDo: Add test for version info.
        }

        /// <summary />
        [TestMethod]
        public void TryRecycle()
        {
            var measurementAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            var counterAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);


            {
                measurementAggregator.Reset(startTS, valueFilter: null);

                measurementAggregator.TrackValue(10);
                measurementAggregator.TrackValue(20);
                measurementAggregator.TrackValue(10);

                ITelemetry aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = measurementAggregator.TryRecycle();
                Assert.IsTrue(canRecycle);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                canRecycle = measurementAggregator.TryRecycle();
                Assert.IsTrue(canRecycle);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
            }
            {
                counterAggregator.Reset(startTS, valueFilter: null);

                counterAggregator.TrackValue(-10);
                counterAggregator.TrackValue(-20);
                counterAggregator.TrackValue(-10);

                ITelemetry aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = counterAggregator.TryRecycle();
                Assert.IsFalse(canRecycle);

                aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
            }
        }

        /// <summary />
        [TestMethod]
        public void GetDataSeries()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregatorForConcreteSeries = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) metric.GetConfiguration(),
                                                    dataSeries: metric,
                                                    consumerKind: MetricConsumerKind.Custom);

            var aggregatorForNullSeries = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

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

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                // Measurement:
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                int filterDoubleInvocationsCount = 0;
                int filterObjectInvocationsCount = 0;

                aggregator.TrackValue("Cow 1");
                aggregator.TrackValue("Cow 2");
                aggregator.TrackValue("Cow 2");

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.Reset(
                               startTS,
                               new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                               filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                               filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; }));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 4");
                aggregator.TrackValue("Cow 4");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(4, filterObjectInvocationsCount);
            }
            {
                // Counter:
                var aggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                int filterDoubleInvocationsCount = 0;
                int filterObjectInvocationsCount = 0;

                aggregator.TrackValue("Cow 1");
                aggregator.TrackValue("Cow 2");
                aggregator.TrackValue("Cow 2");

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.Reset(
                               startTS,
                               new CommonSimpleDataSeriesAggregatorTests.CustomDoubleValueFilter(
                                                                               filterFunctionDouble: (s, v) => { filterDoubleInvocationsCount++; return true; },
                                                                               filterFunctionObject: (s, v) => { filterObjectInvocationsCount++; return true; }));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
                Assert.AreEqual(0, filterDoubleInvocationsCount);
                Assert.AreEqual(0, filterObjectInvocationsCount);

                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 3");
                aggregator.TrackValue("Cow 4");
                aggregator.TrackValue("Cow 4");

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
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
            var measurementMetric = new MetricSeries(aggregationManager, "Unique Cows Sold", mesurementConfig);

            var measurementAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) measurementMetric.GetConfiguration(),
                                                    measurementMetric,
                                                    MetricConsumerKind.Custom);

            var counterConfig = new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: true);
            var counterMetric = new MetricSeries(aggregationManager, "Unique Cows Sold", counterConfig);

            var counterAggregator = new NaiveDistinctCountMetricSeriesAggregator(
                                                    (NaiveDistinctCountMetricSeriesConfiguration) counterMetric.GetConfiguration(),
                                                    counterMetric,
                                                    MetricConsumerKind.Custom);

            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

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

            ITelemetry aggregate = measurementAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterDoubleInvocationsCount);
            Assert.AreEqual(1, filterObjectInvocationsCount);

            measurementAggregator.TrackValue("3");
            measurementAggregator.TrackValue(4);

            aggregate = measurementAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodString);
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
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 3, sum: 2, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(1, filterDoubleInvocationsCount);
            Assert.AreEqual(2, filterObjectInvocationsCount);

            counterAggregator.TrackValue("3");
            counterAggregator.TrackValue(4);

            aggregate = counterAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Unique Cows Sold", count: 5, sum: 4, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodString);
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
