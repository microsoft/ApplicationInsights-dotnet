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
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System.Linq;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    internal static class CommonSimpleDataSeriesAggregatorTests
    {
        public static void CreateAggregateUnsafe(
                                        IMetricSeriesAggregator aggregator,
                                        MetricSeries metric,
                                        IEnumerable<KeyValuePair<string, string>> expectedDimensionNamesValues)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            aggregator.Reset(startTS, valueFilter: null);

#pragma warning disable 618     // Even Obsolete AdditionalDataContext fields must be copied correctly!
            metric.AdditionalDataContext.Cloud.RoleInstance = "A";
            metric.AdditionalDataContext.Cloud.RoleName = "B";
            metric.AdditionalDataContext.Component.Version = "C";
            metric.AdditionalDataContext.Device.Id = "D";
            metric.AdditionalDataContext.Device.Language = "E";
            metric.AdditionalDataContext.Device.Model = "F";
            metric.AdditionalDataContext.Device.NetworkType = "G";
            metric.AdditionalDataContext.Device.OemName = "H";
            metric.AdditionalDataContext.Device.OperatingSystem = "I";
            metric.AdditionalDataContext.Device.ScreenResolution = "J";
            metric.AdditionalDataContext.Device.Type = "K";
            metric.AdditionalDataContext.InstrumentationKey = "L";
            metric.AdditionalDataContext.Location.Ip = "M";
            metric.AdditionalDataContext.Operation.Id = "N";
            metric.AdditionalDataContext.Operation.Name = "O";
            metric.AdditionalDataContext.Operation.ParentId = "P";
            metric.AdditionalDataContext.Operation.SyntheticSource = "Q";
            metric.AdditionalDataContext.Session.Id = "R";
            metric.AdditionalDataContext.Session.IsFirst = true;
            metric.AdditionalDataContext.User.AccountId = "S";
            metric.AdditionalDataContext.User.AuthenticatedUserId = "T";
            metric.AdditionalDataContext.User.Id = "U";
            metric.AdditionalDataContext.User.UserAgent = "V";
#pragma warning restore 618
            metric.AdditionalDataContext.Properties["Prop 1"] = "W";
            metric.AdditionalDataContext.Properties["Prop 2"] = "X";
            metric.AdditionalDataContext.Properties["Dim 1"] = "Y";


            aggregator.TrackValue(42);
            aggregator.TrackValue(43);

            MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
            Assert.IsNotNull(aggregate);

            Assert.AreEqual("Cows Sold", aggregate.MetricId, "aggregate.MetricId mismatch");
            Assert.AreEqual(2, aggregate.AggregateData["Count"], "aggregate.AggregateData[Count] mismatch");
            Assert.AreEqual(85, (double) aggregate.AggregateData["Sum"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[Sum] mismatch");
            Assert.AreEqual(43, (double) aggregate.AggregateData["Max"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[Max] mismatch");
            Assert.AreEqual(42, (double) aggregate.AggregateData["Min"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[Min] mismatch");
            Assert.AreEqual(0.5, (double) aggregate.AggregateData["StdDev"], TestUtil.Util.MaxAllowedPrecisionError, "aggregate.AggregateData[StdDev] mismatch");

            Assert.AreEqual(startTS, aggregate.AggregationPeriodStart, "aggregate.AggregationPeriodStart mismatch");
            Assert.AreEqual(
                        (endTS - startTS).TotalMilliseconds,
                        aggregate.AggregationPeriodDuration.TotalMilliseconds,
                        "aggregate.AggregationPeriodDuration mismatch");

#pragma warning disable 618
            Assert.IsNotNull(aggregate.AdditionalDataContext);
            Assert.IsInstanceOfType(aggregate.AdditionalDataContext, typeof(TelemetryContext));
            Assert.AreEqual("A", ((TelemetryContext) aggregate.AdditionalDataContext).Cloud.RoleInstance);
            Assert.AreEqual("B", ((TelemetryContext) aggregate.AdditionalDataContext).Cloud.RoleName);
            Assert.AreEqual("C", ((TelemetryContext) aggregate.AdditionalDataContext).Component.Version);
            Assert.AreEqual("D", ((TelemetryContext) aggregate.AdditionalDataContext).Device.Id);
            Assert.AreEqual("E", ((TelemetryContext) aggregate.AdditionalDataContext).Device.Language);
            Assert.AreEqual("F", ((TelemetryContext) aggregate.AdditionalDataContext).Device.Model);
            Assert.AreEqual("G", ((TelemetryContext) aggregate.AdditionalDataContext).Device.NetworkType);
            Assert.AreEqual("H", ((TelemetryContext) aggregate.AdditionalDataContext).Device.OemName);
            Assert.AreEqual("I", ((TelemetryContext) aggregate.AdditionalDataContext).Device.OperatingSystem);
            Assert.AreEqual("J", ((TelemetryContext) aggregate.AdditionalDataContext).Device.ScreenResolution);
            Assert.AreEqual("K", ((TelemetryContext) aggregate.AdditionalDataContext).Device.Type);
            Assert.AreEqual("L", ((TelemetryContext) aggregate.AdditionalDataContext).InstrumentationKey);
            Assert.AreEqual("M", ((TelemetryContext) aggregate.AdditionalDataContext).Location.Ip);
            Assert.AreEqual("N", ((TelemetryContext) aggregate.AdditionalDataContext).Operation.Id);
            Assert.AreEqual("O", ((TelemetryContext) aggregate.AdditionalDataContext).Operation.Name);
            Assert.AreEqual("P", ((TelemetryContext) aggregate.AdditionalDataContext).Operation.ParentId);
            Assert.AreEqual("Q", ((TelemetryContext) aggregate.AdditionalDataContext).Operation.SyntheticSource);
            Assert.AreEqual("R", ((TelemetryContext) aggregate.AdditionalDataContext).Session.Id);
            Assert.AreEqual(true, ((TelemetryContext) aggregate.AdditionalDataContext).Session.IsFirst);
            Assert.AreEqual("S", ((TelemetryContext) aggregate.AdditionalDataContext).User.AccountId);
            Assert.AreEqual("T", ((TelemetryContext) aggregate.AdditionalDataContext).User.AuthenticatedUserId);
            Assert.AreEqual("U", ((TelemetryContext) aggregate.AdditionalDataContext).User.Id);
            Assert.AreEqual("V", ((TelemetryContext) aggregate.AdditionalDataContext).User.UserAgent);
#pragma warning restore 618

            Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey("Prop 1"));
            Assert.AreEqual("W", ((TelemetryContext) aggregate.AdditionalDataContext).Properties["Prop 1"]);

            Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey("Prop 2"));
            Assert.AreEqual("X", ((TelemetryContext) aggregate.AdditionalDataContext).Properties["Prop 2"]);

            Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey("Dim 1"));
            Assert.AreEqual("Y", ((TelemetryContext) aggregate.AdditionalDataContext).Properties["Dim 1"]);

            // We checked the explicitly set properties above.
            // But for some reason, TelemetryContext chooses to store some of its explicit members as properties as well.
            // Whatever sense it may or may not make, all we need to verify here is that we have correctly copies ALL properties:

            Assert.AreEqual(metric.AdditionalDataContext.Properties.Count, ((TelemetryContext) aggregate.AdditionalDataContext).Properties.Count);
            foreach (KeyValuePair<string, string> prop in metric.AdditionalDataContext.Properties)
            {
                Assert.IsTrue(((TelemetryContext) aggregate.AdditionalDataContext).Properties.ContainsKey(prop.Key));
                Assert.AreEqual(prop.Value, ((TelemetryContext) aggregate.AdditionalDataContext).Properties[prop.Key]);
            }

            Assert.AreEqual(expectedDimensionNamesValues.Count(), aggregate.Dimensions.Count);

            foreach(KeyValuePair<string, string> dimNameValue in expectedDimensionNamesValues)
            {
                Assert.IsTrue(aggregate.Dimensions.ContainsKey(dimNameValue.Key), $"missing aggregate.Dimensions[{dimNameValue.Key}]");
                Assert.AreEqual(dimNameValue.Value, aggregate.Dimensions[dimNameValue.Key], $"wrong aggregate.Dimensions[{dimNameValue.Key}]");
            }
        }

        
        public static void TryRecycle(IMetricSeriesAggregator measurementAggregator, IMetricSeriesAggregator counterAggregator)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));

            long periodStringDef = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;
            long periodStringStart = (long) (endTS - startTS).TotalMilliseconds;

            {
                measurementAggregator.TrackValue(10);

                MetricAggregate aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 10.0, max: 10.0, min: 10.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                measurementAggregator.Reset(startTS, valueFilter: null);

                measurementAggregator.TrackValue(10);
                measurementAggregator.TrackValue(20);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = measurementAggregator.TryRecycle();

                Assert.IsTrue(canRecycle);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                canRecycle = measurementAggregator.TryRecycle();

                Assert.IsTrue(canRecycle);

                aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
            }
            {
                counterAggregator.TrackValue(10);

                MetricAggregate aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 10.0, max: 10.0, min: 10.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

                counterAggregator.Reset(startTS, valueFilter: null);

                counterAggregator.TrackValue(10);
                counterAggregator.TrackValue(20);

                aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);

                bool canRecycle = counterAggregator.TryRecycle();

                Assert.IsFalse(canRecycle);

                aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
                TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);
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

            long periodStringDef = (long) (endTS - default(DateTimeOffset)).TotalMilliseconds;
            long periodStringStart = (long) (endTS - startTS).TotalMilliseconds;

            int filterInvocationsCount = 0;

            MetricAggregate aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

            aggregator.Reset(startTS, valueFilter: null);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(10);
            aggregator.TrackValue(20);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 30.0, max: 20.0, min: 10.0, stdDev: 5.0, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(0, filterInvocationsCount);

            aggregator.Reset(default(DateTimeOffset), valueFilter: new CustomDoubleValueFilter( (s, v) => { filterInvocationsCount++; return true; } ));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodStringDef);

            aggregator.TrackValue(100);
            aggregator.TrackValue(200);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 300, max: 200, min: 100, stdDev: 50, timestamp: default(DateTimeOffset), periodMs: periodStringDef);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return false; }));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(100);
            aggregator.TrackValue(200);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(4, filterInvocationsCount);

            aggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return true; }));

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: startTS, periodMs: periodStringStart);

            aggregator.TrackValue(100);
            aggregator.TrackValue(200);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 300, max: 200, min: 100, stdDev: 50, timestamp: startTS, periodMs: periodStringStart);
            Assert.AreEqual(6, filterInvocationsCount);
        }

        public static void CompleteAggregation(IMetricSeriesAggregator measurementAggregator, IMetricSeriesAggregator counterAggregator)
        {
            var startTS = new DateTimeOffset(2017, 9, 25, 17, 0, 0, TimeSpan.FromHours(-8));
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            long periodString = (long) (endTS - startTS).TotalMilliseconds;

            int filterInvocationsCount = 0;

            measurementAggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter( (s, v) => { filterInvocationsCount++; return true; }) );

            Assert.AreEqual(0, filterInvocationsCount);

            measurementAggregator.TrackValue(1);
            measurementAggregator.TrackValue("2");

            MetricAggregate aggregate = measurementAggregator.CompleteAggregation(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            measurementAggregator.TrackValue("3");
            measurementAggregator.TrackValue(4);

            aggregate = measurementAggregator.CompleteAggregation(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            aggregate = measurementAggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);


            filterInvocationsCount = 0;

            counterAggregator.Reset(startTS, valueFilter: new CustomDoubleValueFilter((s, v) => { filterInvocationsCount++; return true; }));

            Assert.AreEqual(0, filterInvocationsCount);

            counterAggregator.TrackValue(1);
            counterAggregator.TrackValue("2");

            aggregate = counterAggregator.CompleteAggregation(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 2, sum: 3, max: 2, min: 1, stdDev: 0.5, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(2, filterInvocationsCount);

            counterAggregator.TrackValue("3");
            counterAggregator.TrackValue(4);

            aggregate = counterAggregator.CompleteAggregation(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 4, sum: 10, max: 4, min: 1, stdDev: 1.11803398874989, timestamp: startTS, periodMs: periodString);
            Assert.AreEqual(4, filterInvocationsCount);

            aggregate = counterAggregator.CreateAggregateUnsafe(endTS);
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name: "Cows Sold", count: 4, sum: 10, max: 4, min: 1, stdDev: 1.11803398874989, timestamp: startTS, periodMs: periodString);
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
