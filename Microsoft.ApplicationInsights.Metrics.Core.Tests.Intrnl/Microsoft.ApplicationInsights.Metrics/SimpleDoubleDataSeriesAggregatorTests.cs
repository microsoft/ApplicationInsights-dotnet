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

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class SimpleDoubleDataSeriesAggregatorTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SimpleDoubleDataSeriesAggregator(configuration: null, dataSeries: null, consumerKind: MetricConsumerKind.Custom));

            Assert.ThrowsException<ArgumentException>(() => new SimpleDoubleDataSeriesAggregator(
                                                                           new NaiveDistinctCountMetricSeriesConfiguration(),
                                                                           dataSeries: null,
                                                                           consumerKind: MetricConsumerKind.Custom));

            Assert.ThrowsException<ArgumentException>(() => new SimpleDoubleDataSeriesAggregator(
                                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                                            dataSeries: null,
                                                                            consumerKind: MetricConsumerKind.Custom));

            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);
                Assert.IsNotNull(aggregator);
            }
            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);
                Assert.IsNotNull(aggregator);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueDouble()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                // Empty aggregator:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Zero value:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(0);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Non zero value:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(-42);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: -42.0, max: -42.0, min: -42.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Two values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(-42);
                aggregator.TrackValue(18);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -24.0, max: 18.0, min: -42.0, stdDev: 30.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // 3 values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);
                aggregator.TrackValue(1800000);
                aggregator.TrackValue(0);
                aggregator.TrackValue(-4200000);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: -2400000.0, max: 1800000.0, min: -4200000.0, stdDev: 2513961.018, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // NaNs:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);
                aggregator.TrackValue(Double.NaN);
                aggregator.TrackValue(1);
                aggregator.TrackValue(Double.NaN);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 1, min: 1, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Infinity:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(1);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 1, min: 1, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(0.5);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 1.5, max: 1, min: 0.5, stdDev: 0.25, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.PositiveInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: Double.MaxValue, max: Double.MaxValue, min: 0.5, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Int32.MinValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: Double.MaxValue, max: Double.MaxValue, min: Int32.MinValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.PositiveInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: Double.MaxValue, max: Double.MaxValue, min: Int32.MinValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.NegativeInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 0.0, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(11);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 0.0, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Very large numbers:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Math.Exp(300));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: Math.Exp(300), max: Math.Exp(300), min: Math.Exp(300), stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-2 * Math.Exp(300));
                double minus2exp200 = -2 * Math.Exp(300);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -Math.Exp(300), max: Math.Exp(300), min: minus2exp200, stdDev: 2.91363959286188000E+130, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Math.Exp(300));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 0, max: Math.Exp(300), min: minus2exp200, stdDev: 2.74700575206167000E+130, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Math.Exp(700));

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: Math.Exp(700), max: Math.Exp(700), min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: Double.MaxValue, max: Double.MaxValue, min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(Double.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: Double.MaxValue, max: Double.MaxValue, min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(11);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: Double.MaxValue, max: Double.MaxValue, min: minus2exp200, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-Double.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: Double.MaxValue, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-Double.PositiveInfinity);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 0, max: Double.MaxValue, min: -Double.MaxValue, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Large number of small values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                for (int i = 0; i < 100000; i++)
                {
                    for (double v = 0; v <= 1.0 || Math.Abs(1.0 - v) < Utils.MaxAllowedPrecisionError; v += 0.01)
                    {
                        aggregator.TrackValue(v);
                    }
                }

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10100000, sum: 5050000, max: 1, min: 0, stdDev: 0.29154759474226500, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Large number of large values:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                for (int i = 0; i < 100000; i++)
                {
                    for (double v = 0; v <= 300000.0 || Math.Abs(300000.0 - v) < Utils.MaxAllowedPrecisionError; v += 3000)
                    {
                        aggregator.TrackValue(v);
                    }
                }

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10100000, sum: 1515000000000, max: 300000, min: 0, stdDev: 87464.2784226795, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            aggregator.TrackValue(null);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Boolean) true) );

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (SByte) (0-1));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: -1, max: -1, min: -1, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Byte) 2);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 1, max: 2, min: -1, stdDev: 1.5, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Int16) (0-3));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: -2, max: 2, min: -3, stdDev: 2.05480466765633, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (UInt16) 4);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 2, max: 4, min: -3, stdDev: 2.69258240356725, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Int32) (0-5));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: -3, max: 4, min: -5, stdDev: 3.26190128606002, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (UInt32) 6);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 3, max: 6, min: -5, stdDev: 3.86221007541882, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Int64) (0-7));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: -4, max: 6, min: -7, stdDev: 4.43547848464572, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (UInt64) 8);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 8, sum: 4, max: 8, min: -7, stdDev: 5.02493781056044, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (IntPtr) 0xFF) );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (UIntPtr) 0xFF) );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Char) 'x') );

            aggregator.TrackValue((object) (Single) (0f-9.0f));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: -5, max: 8, min: -9, stdDev: 5.59982363037962000, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue((object) (Double) 10.0);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 10, sum: 5, max: 10, min: -9, stdDev: 6.18465843842649000, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("-11");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 11, sum: -6, max: 10, min: -11, stdDev: 6.76036088821026, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("12.00");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 12, sum: 6, max: 12, min: -11, stdDev: 7.34279692397023, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("-1.300E+01");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 13, sum: -7, max: 12, min: -13, stdDev: 7.91896831484996, timestamp: default(DateTimeOffset), periodMs: periodString);

            aggregator.TrackValue("  +14. ");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 7, max: 14, min: -13, stdDev: 8.5, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("fifteen") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("foo-bar") );

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 14, sum: 7, max: 14, min: -13, stdDev: 8.5, timestamp: default(DateTimeOffset), periodMs: periodString);
           
        }

        private static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, string periodMs)
        {
            Assert.IsNotNull(aggregate);

            MetricTelemetry metricAggregate = aggregate as MetricTelemetry;

            Assert.IsNotNull(metricAggregate);

            Assert.AreEqual(name, metricAggregate.Name, "metricAggregate.Name mismatch");
            Assert.AreEqual(count, metricAggregate.Count, "metricAggregate.Count mismatch");
            Assert.AreEqual(sum, metricAggregate.Sum, Utils.MaxAllowedPrecisionError, "metricAggregate.Sum mismatch");
            Assert.AreEqual(max, metricAggregate.Max.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Max mismatch");
            Assert.AreEqual(min, metricAggregate.Min.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Min mismatch");

            // For very large numbers we perform an approx comparison.
            if (Math.Abs(stdDev) > Int64.MaxValue)
            {
                double expectedStdDevScale = Math.Floor(Math.Log10(Math.Abs(stdDev)));
                double actualStdDevScale = Math.Floor(Math.Log10(Math.Abs(metricAggregate.StandardDeviation.Value)));
                Assert.AreEqual(expectedStdDevScale, actualStdDevScale, "metricAggregate.StandardDeviation (exponent) mismatch");
                Assert.AreEqual(
                            stdDev / Math.Pow(10, expectedStdDevScale),
                            metricAggregate.StandardDeviation.Value / Math.Pow(10, actualStdDevScale),
                            Utils.MaxAllowedPrecisionError,
                            "metricAggregate.StandardDeviation (significant part) mismatch");
            }
            else
            {
                Assert.AreEqual(stdDev, metricAggregate.StandardDeviation.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.StandardDeviation mismatch");
            }
            
            Assert.AreEqual(timestamp, metricAggregate.Timestamp, "metricAggregate.Timestamp mismatch");
            Assert.AreEqual(periodMs, metricAggregate?.Properties?[Utils.AggregationIntervalMonikerPropertyKey], "metricAggregate.Properties[AggregationIntervalMonikerPropertyKey] mismatch");
        }

        /// <summary />
        [TestMethod]
        public void CreateAggregateUnsafe()
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    metric.GetConfiguration(),
                                                    metric,
                                                    MetricConsumerKind.Custom);
            aggregator.Reset(startTS, valueFilter: null);

#pragma warning disable 618     // Even Obsolete Context fields must be copied correctly!
            metric.Context.Cloud.RoleInstance = "A";
            metric.Context.Cloud.RoleName = "B";
            metric.Context.Component.Version = "C";
            metric.Context.Device.Id = "D";
            metric.Context.Device.Language = "E";
            metric.Context.Device.Model = "F";
            metric.Context.Device.NetworkType = "G";
            metric.Context.Device.OemName = "H";
            metric.Context.Device.OperatingSystem = "I";
            metric.Context.Device.ScreenResolution = "J";
            metric.Context.Device.Type = "K";
            metric.Context.InstrumentationKey = "L";
            metric.Context.Location.Ip = "M";
            metric.Context.Operation.Id = "N";
            metric.Context.Operation.Name = "O";
            metric.Context.Operation.ParentId = "P";
            metric.Context.Operation.SyntheticSource = "Q";
            metric.Context.Session.Id = "R";
            metric.Context.Session.IsFirst = true;
            metric.Context.User.AccountId = "S";
            metric.Context.User.AuthenticatedUserId = "T";
            metric.Context.User.Id = "U";
            metric.Context.User.UserAgent = "V";
#pragma warning restore 618
            metric.Context.Properties["Dim 1"] = "W";
            metric.Context.Properties["Dim 2"] = "X";
            metric.Context.Properties["Dim 3"] = "Y";


            aggregator.TrackValue(42);
            aggregator.TrackValue(42.42);

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            Assert.IsNotNull(aggregate);

            MetricTelemetry metricAggregate = aggregate as MetricTelemetry;
            Assert.IsNotNull(metricAggregate);

            Assert.AreEqual("Cows Sold", metricAggregate.Name, "metricAggregate.Name mismatch");
            Assert.AreEqual(2, metricAggregate.Count, "metricAggregate.Count mismatch");
            Assert.AreEqual(84.42, metricAggregate.Sum, Utils.MaxAllowedPrecisionError, "metricAggregate.Sum mismatch");
            Assert.AreEqual(42.42, metricAggregate.Max.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Max mismatch");
            Assert.AreEqual(42.00, metricAggregate.Min.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Min mismatch");
            Assert.AreEqual(0.21, metricAggregate.StandardDeviation.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.StandardDeviation mismatch");

            Assert.AreEqual(startTS, metricAggregate.Timestamp, "metricAggregate.Timestamp mismatch");
            Assert.AreEqual(
                        ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture),
                        metricAggregate?.Properties?[Utils.AggregationIntervalMonikerPropertyKey],
                        "metricAggregate.Properties[AggregationIntervalMonikerPropertyKey] mismatch");

#pragma warning disable 618
            Assert.AreEqual("A", metricAggregate.Context.Cloud.RoleInstance);
            Assert.AreEqual("B", metricAggregate.Context.Cloud.RoleName);
            Assert.AreEqual("C", metricAggregate.Context.Component.Version);
            Assert.AreEqual("D", metricAggregate.Context.Device.Id);
            Assert.AreEqual("E", metricAggregate.Context.Device.Language);
            Assert.AreEqual("F", metricAggregate.Context.Device.Model);
            Assert.AreEqual("G", metricAggregate.Context.Device.NetworkType);
            Assert.AreEqual("H", metricAggregate.Context.Device.OemName);
            Assert.AreEqual("I", metricAggregate.Context.Device.OperatingSystem);
            Assert.AreEqual("J", metricAggregate.Context.Device.ScreenResolution);
            Assert.AreEqual("K", metricAggregate.Context.Device.Type);
            Assert.AreEqual(String.Empty, metricAggregate.Context.InstrumentationKey);
            Assert.AreEqual("M", metricAggregate.Context.Location.Ip);
            Assert.AreEqual("N", metricAggregate.Context.Operation.Id);
            Assert.AreEqual("O", metricAggregate.Context.Operation.Name);
            Assert.AreEqual("P", metricAggregate.Context.Operation.ParentId);
            Assert.AreEqual("Q", metricAggregate.Context.Operation.SyntheticSource);
            Assert.AreEqual("R", metricAggregate.Context.Session.Id);
            Assert.AreEqual(true, metricAggregate.Context.Session.IsFirst);
            Assert.AreEqual("S", metricAggregate.Context.User.AccountId);
            Assert.AreEqual("T", metricAggregate.Context.User.AuthenticatedUserId);
            Assert.AreEqual("U", metricAggregate.Context.User.Id);
            Assert.AreEqual("V", metricAggregate.Context.User.UserAgent);
#pragma warning restore 618

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
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(-10);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: -10.0, max: -10.0, min: -10.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                aggregator.Reset(startTS, valueFilter: null);

                aggregator.TrackValue(-10);
                aggregator.TrackValue(-20);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -30.0, max: -10.0, min: -20.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = aggregator.TryRecycle();

                Assert.IsTrue(canRecycle);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                canRecycle = aggregator.TryRecycle();

                Assert.IsTrue(canRecycle);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
            }
            {
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

                aggregator.TrackValue(-10);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: -10.0, max: -10.0, min: -10.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                aggregator.Reset(startTS, valueFilter: null);

                aggregator.TrackValue(-10);
                aggregator.TrackValue(-20);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -30.0, max: -10.0, min: -20.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = aggregator.TryRecycle();

                Assert.IsFalse(canRecycle);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -30.0, max: -10.0, min: -20.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);
            }
        }

        /// <summary />
        [TestMethod]
        public void GetDataSeries()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregator1 = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: metric,
                                                    consumerKind: MetricConsumerKind.Custom);

            var aggregator2 = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    consumerKind: MetricConsumerKind.Custom);

            Assert.IsNotNull(aggregator1.DataSeries);
            Assert.AreSame(metric, aggregator1.DataSeries);
            Assert.IsNull(aggregator2.DataSeries);
        }

        /// <summary />
        [TestMethod]
        public void Reset()
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            int filterInvocationsCount = 0;

            var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                dataSeries: null,
                                                consumerKind: MetricConsumerKind.Custom);

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

            aggregator.Reset(startTS, valueFilter: null);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(-10);
            aggregator.TrackValue(-20);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -30.0, max: -10.0, min: -20.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(0, filterInvocationsCount);

            aggregator.Reset(default(DateTimeOffset), valueFilter: new CustomDoubleValueFilter( (s, v) => { filterInvocationsCount++; return true; } ));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

            aggregator.TrackValue(-0.10);
            aggregator.TrackValue(-0.20);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -0.3, max: -0.1, min: -0.2, stdDev: 0.05, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return false; }));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(-0.10);
            aggregator.TrackValue(-0.20);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(4, filterInvocationsCount);

            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return true; }));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(-0.10);
            aggregator.TrackValue(-0.20);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: -0.3, max: -0.1, min: -0.2, stdDev: 0.05, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(6, filterInvocationsCount);
        }

        /// <summary />
        [TestMethod]
        public void CompleteAggregation()
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            int filterInvocationsCount = 0;

            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    metric.GetConfiguration(),
                                                    metric,
                                                    MetricConsumerKind.Custom);
            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter( (s, v) => { filterInvocationsCount++; return true; }) );

            Assert.AreEqual(0, filterInvocationsCount);

            aggregator.TrackValue(1);
            aggregator.TrackValue("2");

            ITelemetry aggregate = aggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregator.TrackValue("3");
            aggregator.TrackValue(4);

            aggregate = aggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
        }

        private class CustomDoubleValueFilter : IMetricValueFilter
        {
            public CustomDoubleValueFilter(Func<MetricSeries, double, bool> filterFunction)
            {
                FilterFunction = filterFunction;
            }

            public Func<MetricSeries, double, bool> FilterFunction { get; }

            public bool WillConsume(MetricSeries dataSeries, double metricValue)
            {
                if (FilterFunction == null)
                {
                    return true;
                }

                return FilterFunction(dataSeries, metricValue);
            }

            public bool WillConsume(MetricSeries dataSeries, object metricValue)
            {
                if (FilterFunction == null)
                {
                    return true;
                }

                if (metricValue == null)
                {
                    return false;
                }

                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    double doubleValue;
                    if (Double.TryParse(stringValue, out doubleValue))
                    {
                        return WillConsume(dataSeries, doubleValue);
                    }
                    else
                    {
                        return false;
                    }
                }

                try
                {
                    double doubleValue = (double) metricValue;
                    return WillConsume(dataSeries, doubleValue);
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }
        }
    }
}
