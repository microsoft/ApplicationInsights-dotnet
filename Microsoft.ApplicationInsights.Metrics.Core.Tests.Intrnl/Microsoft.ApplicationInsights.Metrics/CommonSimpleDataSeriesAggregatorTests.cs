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
    internal static class CommonSimpleDataSeriesAggregatorTests
    {
        public static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, string periodMs)
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

        public static void CreateAggregateUnsafe(IMetricSeriesAggregator aggregator, MetricSeries metric)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

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
            aggregator.TrackValue(43);

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            Assert.IsNotNull(aggregate);

            MetricTelemetry metricAggregate = aggregate as MetricTelemetry;
            Assert.IsNotNull(metricAggregate);

            Assert.AreEqual("Cows Sold", metricAggregate.Name, "metricAggregate.Name mismatch");
            Assert.AreEqual(2, metricAggregate.Count, "metricAggregate.Count mismatch");
            Assert.AreEqual(85, metricAggregate.Sum, Utils.MaxAllowedPrecisionError, "metricAggregate.Sum mismatch");
            Assert.AreEqual(43, metricAggregate.Max.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Max mismatch");
            Assert.AreEqual(42, metricAggregate.Min.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.Min mismatch");
            Assert.AreEqual(0.5, metricAggregate.StandardDeviation.Value, Utils.MaxAllowedPrecisionError, "metricAggregate.StandardDeviation mismatch");

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

        
        public static void TryRecycle(IMetricSeriesAggregator measurementAggregator, IMetricSeriesAggregator counterAggregator)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                measurementAggregator.TrackValue(10);

                ITelemetry aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 10.0, max: 10.0, min: 10.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                measurementAggregator.Reset(startTS, valueFilter: null);

                measurementAggregator.TrackValue(10);
                measurementAggregator.TrackValue(20);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);

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
                counterAggregator.TrackValue(10);

                ITelemetry aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 10.0, max: 10.0, min: 10.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                counterAggregator.Reset(startTS, valueFilter: null);

                counterAggregator.TrackValue(10);
                counterAggregator.TrackValue(20);

                aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = counterAggregator.TryRecycle();

                Assert.IsFalse(canRecycle);

                aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);
            }
        }

        public static void GetDataSeries(IMetricSeriesAggregator aggregatorForConcreteSeries, IMetricSeriesAggregator aggregatorForNullSeries, MetricSeries series)
        {
            Assert.IsNotNull(aggregatorForConcreteSeries.DataSeries);
            Assert.AreSame(series, aggregatorForConcreteSeries.DataSeries);

            Assert.IsNull(aggregatorForNullSeries.DataSeries);
        }

        public static void Reset(IMetricSeriesAggregator aggregator)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            var periodStringDef = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
            var periodStringStart = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            int filterInvocationsCount = 0;

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

            aggregator.Reset(startTS, valueFilter: null);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(10);
            aggregator.TrackValue(20);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(0, filterInvocationsCount);

            aggregator.Reset(default(DateTimeOffset), valueFilter: new CustomDoubleValueFilter( (s, v) => { filterInvocationsCount++; return true; } ));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

            aggregator.TrackValue(100);
            aggregator.TrackValue(200);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 300, max: 200, min: 100, stdDev: 50, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return false; }));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(100);
            aggregator.TrackValue(200);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(4, filterInvocationsCount);

            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return true; }));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(100);
            aggregator.TrackValue(200);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 300, max: 200, min: 100, stdDev: 50, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(6, filterInvocationsCount);
        }

        public static void CompleteAggregation(IMetricSeriesAggregator measurementAggregator, IMetricSeriesAggregator counterAggregator)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - startTS).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            int filterInvocationsCount = 0;

            measurementAggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter( (s, v) => { filterInvocationsCount++; return true; }) );

            Assert.AreEqual(0, filterInvocationsCount);

            measurementAggregator.TrackValue(1);
            measurementAggregator.TrackValue("2");

            ITelemetry aggregate = measurementAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            measurementAggregator.TrackValue("3");
            measurementAggregator.TrackValue(4);

            aggregate = measurementAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);


            filterInvocationsCount = 0;

            counterAggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return true; }));

            Assert.AreEqual(0, filterInvocationsCount);

            counterAggregator.TrackValue(1);
            counterAggregator.TrackValue("2");

            aggregate = counterAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            counterAggregator.TrackValue("3");
            counterAggregator.TrackValue(4);

            aggregate = counterAggregator.CompleteAggregation(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 4, sum: 10, max: 4, min: 1, stdDev: 1.11803398874989, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(4, filterInvocationsCount);

            aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 4, sum: 10, max: 4, min: 1, stdDev: 1.11803398874989, timestamp: startTS, periodMs: periodString);
        }

        #region class CustomDoubleValueFilter
        internal class CustomDoubleValueFilter : IMetricValueFilter
        {
            public CustomDoubleValueFilter(Func<MetricSeries, double, bool> filterFunctionDouble)
            {
                FilterFunctionDouble = filterFunctionDouble;
                FilterFunctionObject = InterpretObjectAsDoubleFilter;
            }

            public CustomDoubleValueFilter(
                                Func<MetricSeries, double, bool> filterFunctionDouble,
                                Func<MetricSeries, object, bool> filterFunctionObject)
            {
                FilterFunctionDouble = filterFunctionDouble;
                FilterFunctionObject = filterFunctionObject;
            }

            public Func<MetricSeries, double, bool> FilterFunctionDouble { get; }
            public Func<MetricSeries, object, bool> FilterFunctionObject { get; }

            public bool WillConsume(MetricSeries dataSeries, double metricValue)
            {
                if (FilterFunctionDouble == null)
                {
                    return true;
                }

                return FilterFunctionDouble(dataSeries, metricValue);
            }

            public bool WillConsume(MetricSeries dataSeries, object metricValue)
            {
                if (FilterFunctionObject == null)
                {
                    return true;
                }

                return FilterFunctionObject(dataSeries, metricValue);
            }

            private bool InterpretObjectAsDoubleFilter(MetricSeries dataSeries, object metricValue)
            {
                if (metricValue == null)
                {
                    return WillConsume(dataSeries, Double.NaN);
                }

                double doubleValue;

                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    if (!Double.TryParse(stringValue, out doubleValue))
                    {
                        doubleValue = Double.NaN;
                    }
                }
                else
                {
                    try
                    {
                        doubleValue = (double) metricValue;
                    }
                    catch (InvalidCastException)
                    {
                        doubleValue = Double.NaN;
                    }
                }

                return WillConsume(dataSeries, doubleValue);
            }
        }
        #endregion class CustomDoubleValueFilter
    }
}
