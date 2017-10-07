using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricSeriesTests
    {
        /// <summary />
        [TestMethod]
        public void Properties()
        {
            var manager = new MetricManager(new MemoryMetricTelemetryPipeline());
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            Assert.AreEqual("Foo Bar", series.MetricId);

            Assert.IsNotNull(series.Context);
            Assert.IsNotNull(series.Context.Properties);
            Assert.AreEqual(0, series.Context.Properties.Count);

            Assert.AreEqual(config, series.GetConfiguration());
            Assert.AreSame(config, series.GetConfiguration());
        }

        /// <summary />
        [TestMethod]
        public void TrackValueDouble()
        {
            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            series.TrackValue(0.4);
            series.TrackValue(0.8);
            series.TrackValue(-0.04);

            Assert.AreEqual(0, aggregateCollector.Count);
            manager.Flush();
            Assert.AreEqual(1, aggregateCollector.Count);

            DateTimeOffset endTS = DateTimeOffset.Now;

            Assert.IsInstanceOfType(aggregateCollector[0], typeof(MetricTelemetry));

            Util.ValidateNumericAggregateValues((ITelemetry) aggregateCollector[0], name: "Foo Bar", count: 3, sum: 1.16, max: 0.8, min: -0.04, stdDev: 0.343058142140496);

            // Timestamp checks have to be approximate, since we have no possibilityt to get exact timetamps snapped internally.

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        ((ITelemetry) aggregateCollector[0]).Timestamp);

            const int millisecsTollerance = 50;
            string durationMs = ((ITelemetry) aggregateCollector[0]).Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey];
            Assert.IsNotNull(durationMs);
            Assert.IsTrue(Math.Abs(Int64.Parse(durationMs) - (endTS - ((ITelemetry) aggregateCollector[0]).Timestamp).TotalMilliseconds) < millisecsTollerance);

            Assert.AreEqual(1, aggregateCollector.Count);
            aggregateCollector.Clear();
            Assert.AreEqual(0, aggregateCollector.Count);

            manager.Flush();
            Assert.AreEqual(0, aggregateCollector.Count);
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            Assert.ThrowsException<ArgumentException>( () => series.TrackValue("xxx") );
            series.TrackValue((float) 0.8);
            series.TrackValue(-0.04);
            series.TrackValue("0.4");

            Assert.AreEqual(0, aggregateCollector.Count);
            manager.Flush();
            Assert.AreEqual(1, aggregateCollector.Count);

            DateTimeOffset endTS = DateTimeOffset.Now;

            Assert.IsInstanceOfType(aggregateCollector[0], typeof(MetricTelemetry));

            Util.ValidateNumericAggregateValues((ITelemetry) aggregateCollector[0], name: "Foo Bar", count: 3, sum: 1.16, max: 0.8, min: -0.04, stdDev: 0.343058142140496);

            // Timestamp checks have to be approximate, since we have no possibilityt to get exact timetamps snapped internally.

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        ((ITelemetry) aggregateCollector[0]).Timestamp);

            const int millisecsTollerance = 50;
            string durationMs = ((ITelemetry) aggregateCollector[0]).Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey];
            Assert.IsNotNull(durationMs);
            Assert.IsTrue(Math.Abs(Int64.Parse(durationMs) - (endTS - ((ITelemetry) aggregateCollector[0]).Timestamp).TotalMilliseconds) < millisecsTollerance);
        }

        /// <summary />
        [TestMethod]
        public void ResetAggregation()
        {
            // Do not start this test in the last 10 secs or first 2 secs of a minute, to make sure the timings below are likely to work out.

            while (DateTimeOffset.Now.Second >= 50 || DateTimeOffset.Now.Second < 2)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            series.TrackValue(0.4);
            series.TrackValue(2);
            series.TrackValue(-2);

            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            DateTimeOffset resetTS = DateTimeOffset.Now;
            series.ResetAggregation();

            series.TrackValue(0.17);
            series.TrackValue(0.32);
            series.TrackValue(-0.15);
            series.TrackValue(1.07);

            Assert.AreEqual(0, aggregateCollector.Count);
            manager.Flush();
            Assert.AreEqual(1, aggregateCollector.Count);

            DateTimeOffset endTS = DateTimeOffset.Now;

            Assert.IsInstanceOfType(aggregateCollector[0], typeof(MetricTelemetry));

            Util.ValidateNumericAggregateValues((ITelemetry) aggregateCollector[0], name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreNotEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        ((ITelemetry) aggregateCollector[0]).Timestamp);

            Assert.AreEqual(
                        new DateTimeOffset(resetTS.Year, resetTS.Month, resetTS.Day, resetTS.Hour, resetTS.Minute, resetTS.Second, 0, resetTS.Offset),
                        ((ITelemetry) aggregateCollector[0]).Timestamp);
            
        }

        /// <summary />
        [TestMethod]
        public void ResetAggregationDateTimeOffset()
        {
            // Do not start this test in the last 10 secs or first 2 secs of a minute, to make sure the timings below are likely to work out.

            while (DateTimeOffset.Now.Second >= 49 || DateTimeOffset.Now.Second < 3)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            series.TrackValue(0.4);
            series.TrackValue(2);
            series.TrackValue(-2);

            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            DateTimeOffset resetTS = DateTimeOffset.Now;
            series.ResetAggregation(resetTS);

            series.TrackValue(0.17);
            series.TrackValue(0.32);
            series.TrackValue(-0.15);
            series.TrackValue(1.07);

            Assert.AreEqual(0, aggregateCollector.Count);
            manager.Flush();
            Assert.AreEqual(1, aggregateCollector.Count);

            DateTimeOffset endTS = DateTimeOffset.Now;

            Assert.IsInstanceOfType(aggregateCollector[0], typeof(MetricTelemetry));

            Util.ValidateNumericAggregateValues((ITelemetry) aggregateCollector[0], name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreNotEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        ((ITelemetry) aggregateCollector[0]).Timestamp);

            Assert.AreEqual(
                        new DateTimeOffset(resetTS.Year, resetTS.Month, resetTS.Day, resetTS.Hour, resetTS.Minute, resetTS.Second, 0, resetTS.Offset),
                        ((ITelemetry) aggregateCollector[0]).Timestamp);

        }

        /// <summary />
        [TestMethod]
        public void GetCurrentAggregateUnsafe()
        {
            GetCurrentAggregateUnsafeTest(new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));
            GetCurrentAggregateUnsafeTest(new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));
        }

        private static void GetCurrentAggregateUnsafeTest(IMetricSeriesConfiguration seriesConfig)
        { 
            // Do not start this test in the last 10 secs or first 2 secs of a minute, to make sure the timings below are likely to work out.

            while (DateTimeOffset.Now.Second >= 49 || DateTimeOffset.Now.Second < 3)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", seriesConfig);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsNull(aggregate);
            }

            series.TrackValue(0.4);
            series.TrackValue(2);
            series.TrackValue(-2);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.Timestamp);
            }

            series.TrackValue(0.17);
            series.TrackValue(0.32);
            series.TrackValue(-0.15);
            series.TrackValue(1.07);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.Timestamp);
            }

            Thread.Sleep(1500);
            DateTimeOffset flushTS = DateTimeOffset.Now;
            manager.Flush();

            if (seriesConfig.RequiresPersistentAggregation)
            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.Timestamp);
            }
            else
            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsNull(aggregate);
            }

            series.TrackValue(0);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);
                }
                else
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 1, sum: 0, max: 0, min: 0, stdDev: 0);
                }

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.Timestamp);
                }
                else
                {
                    Assert.AreEqual(
                             new DateTimeOffset(flushTS.Year, flushTS.Month, flushTS.Day, flushTS.Hour, flushTS.Minute, flushTS.Second, 0, flushTS.Offset),
                             aggregate.Timestamp);
                }
            }
        }

        /// <summary />
        [TestMethod]
        public void GetCurrentAggregateUnsafe_MetricAggregationCycleKind_DateTimeOffset()
        {
            GetCurrentAggregateUnsafeTest_MetricAggregationCycleKind_DateTimeOffset(new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));
            GetCurrentAggregateUnsafeTest_MetricAggregationCycleKind_DateTimeOffset(new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));
        }

        private static void GetCurrentAggregateUnsafeTest_MetricAggregationCycleKind_DateTimeOffset(IMetricSeriesConfiguration seriesConfig)
        { 
            // Do not start this test in the last 10 secs or first 2 secs of a minute, to make sure the timings below are likely to work out.

            while (DateTimeOffset.Now.Second >= 49 || DateTimeOffset.Now.Second < 3)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", seriesConfig);

            DateTimeOffset stepTS = startTS.AddMinutes(2);
            DateTimeOffset stepTSRounded = new DateTimeOffset(stepTS.Year, stepTS.Month, stepTS.Day, stepTS.Hour, stepTS.Minute, stepTS.Second, 0, stepTS.Offset);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);
                Assert.IsNull(aggregate);

                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);
                Assert.IsNull(aggregate);

                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);
                Assert.IsNull(aggregate);
            }

            series.TrackValue(0.4);
            series.TrackValue(2);
            series.TrackValue(-2);

            stepTS = stepTS.AddMinutes(2);
            stepTSRounded = new DateTimeOffset(stepTS.Year, stepTS.Month, stepTS.Day, stepTS.Hour, stepTS.Minute, stepTS.Second, 0, stepTS.Offset);


            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.Timestamp);

                Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);


                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);
                    Assert.IsNull(aggregate);

                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);
                    Assert.IsNull(aggregate);
                }
            }

            DateTimeOffset customCycleStartTS = stepTS.AddMinutes(1);
            manager.StartOrCycleAggregators(MetricAggregationCycleKind.Custom, customCycleStartTS, futureFilter: null);

            series.TrackValue(0.17);
            series.TrackValue(0.32);
            series.TrackValue(-0.15);
            series.TrackValue(1.07);

            stepTS = stepTS.AddMinutes(2);
            stepTSRounded = new DateTimeOffset(stepTS.Year, stepTS.Month, stepTS.Day, stepTS.Hour, stepTS.Minute, stepTS.Second, 0, stepTS.Offset);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.Timestamp);

                Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(customCycleStartTS.Year, customCycleStartTS.Month, customCycleStartTS.Day, customCycleStartTS.Hour, customCycleStartTS.Minute, customCycleStartTS.Second, 0, customCycleStartTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }

                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);
                Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    Assert.IsNull(aggregate);
                }
            }

            Thread.Sleep(1500);
            DateTimeOffset flushTS = DateTimeOffset.Now;
            manager.Flush();


            DateTimeOffset quickPulseCycleStartTS = stepTS.AddMinutes(1);
            manager.StartOrCycleAggregators(MetricAggregationCycleKind.QuickPulse, quickPulseCycleStartTS, futureFilter: null);

            stepTS = stepTS.AddMinutes(2);
            stepTSRounded = new DateTimeOffset(stepTS.Year, stepTS.Month, stepTS.Day, stepTS.Hour, stepTS.Minute, stepTS.Second, 0, stepTS.Offset);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    Assert.IsNull(aggregate);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    // Custom was not cycled by Flush.
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                                new DateTimeOffset(customCycleStartTS.Year, customCycleStartTS.Month, customCycleStartTS.Day, customCycleStartTS.Hour, customCycleStartTS.Minute, customCycleStartTS.Second, 0, customCycleStartTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);

                    manager.StartOrCycleAggregators(MetricAggregationCycleKind.Custom, flushTS, null);

                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);
                    Assert.IsNull(aggregate);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));

                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.Timestamp);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                                aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    // We started QP cycle now, but we did not write values since then.
                    Assert.IsNull(aggregate);
                }
            }

            series.TrackValue(0);

            stepTS = stepTS.AddMinutes(2);
            stepTSRounded = new DateTimeOffset(stepTS.Year, stepTS.Month, stepTS.Day, stepTS.Hour, stepTS.Minute, stepTS.Second, 0, stepTS.Offset);

            manager.StopAggregators(MetricAggregationCycleKind.Custom, stepTS);

            {
                ITelemetry aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.Timestamp);

                    Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 1, sum: 0, max: 0, min: 0, stdDev: 0);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(flushTS.Year, flushTS.Month, flushTS.Day, flushTS.Hour, flushTS.Minute, flushTS.Second, 0, flushTS.Offset),
                             aggregate.Timestamp);

                    Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.Timestamp);

                    Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    Assert.IsNull(aggregate);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.Timestamp);

                    Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
                else
                {
                    Assert.IsInstanceOfType(aggregate, typeof(MetricTelemetry));
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 1, sum: 0, max: 0, min: 0, stdDev: 0);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(quickPulseCycleStartTS.Year, quickPulseCycleStartTS.Month, quickPulseCycleStartTS.Day, quickPulseCycleStartTS.Hour, quickPulseCycleStartTS.Minute, quickPulseCycleStartTS.Second, 0, quickPulseCycleStartTS.Offset),
                             aggregate.Timestamp);

                    Assert.AreEqual(
                            (stepTSRounded - aggregate.Timestamp).TotalMilliseconds.ToString(),
                            aggregate.Context?.Properties?[Util.AggregationIntervalMonikerPropertyKey]);
                }
            }
        }
    }
}
