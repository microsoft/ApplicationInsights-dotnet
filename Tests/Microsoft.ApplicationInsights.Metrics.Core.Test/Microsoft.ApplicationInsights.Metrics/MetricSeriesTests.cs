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
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void Properties()
        {
            var manager = new MetricManager(new MemoryMetricTelemetryPipeline());
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            Assert.AreEqual("Foo Bar", series.MetricId);

            Assert.AreEqual(config, series.GetConfiguration());
            Assert.AreSame(config, series.GetConfiguration());

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TrackValueDouble()
        {
            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            Thread.Sleep(1500);

            series.TrackValue(0.4);
            series.TrackValue(0.8);
            series.TrackValue(-0.04);

            Assert.AreEqual(0, aggregateCollector.Count);
            manager.Flush();
            Assert.AreEqual(1, aggregateCollector.Count);

            DateTimeOffset endTSRounded = DateTimeOffset.Now;
            endTSRounded = new DateTimeOffset(endTSRounded.Year, endTSRounded.Month, endTSRounded.Day, endTSRounded.Hour, endTSRounded.Minute, endTSRounded.Second, 0, endTSRounded.Offset);

            Util.ValidateNumericAggregateValues(aggregateCollector[0], name: "Foo Bar", count: 3, sum: 1.16, max: 0.8, min: -0.04, stdDev: 0.343058142140496);

            // Timestamp checks have to be approximate, since we have no possibilityt to get exact timetamps snapped internally.

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        aggregateCollector[0].AggregationPeriodStart);

            const int millisecsTollerance = 50;
            long durationMs = (long) aggregateCollector[0].AggregationPeriodDuration.TotalMilliseconds;

            Assert.IsTrue(Math.Abs(durationMs - (endTSRounded - aggregateCollector[0].AggregationPeriodStart).TotalMilliseconds) < millisecsTollerance);

            Assert.AreEqual(1, aggregateCollector.Count);
            aggregateCollector.Clear();
            Assert.AreEqual(0, aggregateCollector.Count);

            manager.Flush();
            Assert.AreEqual(0, aggregateCollector.Count);

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TrackValueObject()
        {
            DateTimeOffset startTS = DateTimeOffset.Now;

            var aggregateCollector = new MemoryMetricTelemetryPipeline();
            var manager = new MetricManager(aggregateCollector);
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false);
            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);

            Assert.ThrowsException<ArgumentException>( () => series.TrackValue("xxx") );
            series.TrackValue((float) 0.8);
            series.TrackValue(-0.04);
            series.TrackValue("0.4");

            Thread.Sleep(1500);

            Assert.AreEqual(0, aggregateCollector.Count);
            manager.Flush();
            Assert.AreEqual(1, aggregateCollector.Count);

            DateTimeOffset endTSRounded = DateTimeOffset.Now;
            endTSRounded = new DateTimeOffset(endTSRounded.Year, endTSRounded.Month, endTSRounded.Day, endTSRounded.Hour, endTSRounded.Minute, endTSRounded.Second, 0, endTSRounded.Offset);

            Util.ValidateNumericAggregateValues(aggregateCollector[0], name: "Foo Bar", count: 3, sum: 1.16, max: 0.8, min: -0.04, stdDev: 0.343058142140496);

            // Timestamp checks have to be approximate, since we have no possibilityt to get exact timetamps snapped internally.

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        aggregateCollector[0].AggregationPeriodStart);

            const int millisecsTollerance = 50;
            long durationMs = (long) aggregateCollector[0].AggregationPeriodDuration.TotalMilliseconds;
            Assert.IsTrue(Math.Abs(durationMs - (endTSRounded - aggregateCollector[0].AggregationPeriodStart).TotalMilliseconds) < millisecsTollerance);

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
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
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false);
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

            Util.ValidateNumericAggregateValues(aggregateCollector[0], name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreNotEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        aggregateCollector[0].AggregationPeriodStart);

            Assert.AreEqual(
                        new DateTimeOffset(resetTS.Year, resetTS.Month, resetTS.Day, resetTS.Hour, resetTS.Minute, resetTS.Second, 0, resetTS.Offset),
                        aggregateCollector[0].AggregationPeriodStart);

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
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
            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false);
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

            Util.ValidateNumericAggregateValues(aggregateCollector[0], name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

            // The following might break sometimes!
            // There is a little chance that second boundary is crossed between startTS and the aggregation timestamps are snapped.
            // rerun the test if it happens.

            Assert.AreNotEqual(
                        new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                        aggregateCollector[0].AggregationPeriodStart);

            Assert.AreEqual(
                        new DateTimeOffset(resetTS.Year, resetTS.Month, resetTS.Day, resetTS.Hour, resetTS.Minute, resetTS.Second, 0, resetTS.Offset),
                        aggregateCollector[0].AggregationPeriodStart);

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetCurrentAggregateUnsafe()
        {
            GetCurrentAggregateUnsafeTest(new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false));
            GetCurrentAggregateUnsafeTest(new SimpleMetricSeriesConfiguration(usePersistentAggregation: true, restrictToUInt32Values: false));
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
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsNull(aggregate);
            }

            series.TrackValue(0.4);
            series.TrackValue(2);
            series.TrackValue(-2);

            {
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe();
                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.AggregationPeriodStart);
            }

            series.TrackValue(0.17);
            series.TrackValue(0.32);
            series.TrackValue(-0.15);
            series.TrackValue(1.07);

            {
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe();

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.AggregationPeriodStart);
            }

            Thread.Sleep(1500);
            DateTimeOffset flushTS = DateTimeOffset.Now;
            manager.Flush();

            if (seriesConfig.RequiresPersistentAggregation)
            {
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe();

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                // The following might break sometimes!
                // There is a little chance that second boundary is crossed between test TS and the aggregation timestamps are snapped.
                // rerun the test if it happens.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.AggregationPeriodStart);
            }
            else
            {
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe();
                Assert.IsNull(aggregate);
            }

            series.TrackValue(0);

            {
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe();

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
                             aggregate.AggregationPeriodStart);
                }
                else
                {
                    Assert.AreEqual(
                             new DateTimeOffset(flushTS.Year, flushTS.Month, flushTS.Day, flushTS.Hour, flushTS.Minute, flushTS.Second, 0, flushTS.Offset),
                             aggregate.AggregationPeriodStart);
                }
            }

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetCurrentAggregateUnsafe_MetricAggregationCycleKind_DateTimeOffset()
        {
            GetCurrentAggregateUnsafeTest_MetricAggregationCycleKind_DateTimeOffset(new SimpleMetricSeriesConfiguration(usePersistentAggregation: false, restrictToUInt32Values: false));
            GetCurrentAggregateUnsafeTest_MetricAggregationCycleKind_DateTimeOffset(new SimpleMetricSeriesConfiguration(usePersistentAggregation: true, restrictToUInt32Values: false));
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
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);
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
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.AggregationPeriodStart);

                Assert.AreEqual(
                            (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                            aggregate.AggregationPeriodDuration.TotalMilliseconds);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);


                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 3, sum: 0.4, max: 2, min: -2, stdDev: 1.64384373412506);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
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
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);

                Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                Assert.AreEqual(
                            new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                            aggregate.AggregationPeriodStart);

                Assert.AreEqual(
                            (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                            aggregate.AggregationPeriodDuration.TotalMilliseconds);


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
                else
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(customCycleStartTS.Year, customCycleStartTS.Month, customCycleStartTS.Day, customCycleStartTS.Hour, customCycleStartTS.Minute, customCycleStartTS.Second, 0, customCycleStartTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }

                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
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
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
                else
                {
                    Assert.IsNull(aggregate);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
                else
                {
                    // Custom was not cycled by Flush.
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 4, sum: 1.41, max: 1.07, min: -0.15, stdDev: 0.447681527427702);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                                new DateTimeOffset(customCycleStartTS.Year, customCycleStartTS.Month, customCycleStartTS.Day, customCycleStartTS.Hour, customCycleStartTS.Minute, customCycleStartTS.Second, 0, customCycleStartTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);

                    manager.StartOrCycleAggregators(MetricAggregationCycleKind.Custom, flushTS, null);

                    aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);
                    Assert.IsNull(aggregate);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 7, sum: 1.81, max: 2, min: -2, stdDev: 1.13330652229191);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.

                    Assert.AreEqual(
                                new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                                aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
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
                MetricAggregate aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Default, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
                else
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 1, sum: 0, max: 0, min: 0, stdDev: 0);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(flushTS.Year, flushTS.Month, flushTS.Day, flushTS.Hour, flushTS.Minute, flushTS.Second, 0, flushTS.Offset),
                             aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.Custom, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
                else
                {
                    Assert.IsNull(aggregate);
                }


                aggregate = series.GetCurrentAggregateUnsafe(MetricAggregationCycleKind.QuickPulse, stepTS);

                if (seriesConfig.RequiresPersistentAggregation)
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 8, sum: 1.81, max: 2, min: -2, stdDev: 1.06355462365597);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(startTS.Year, startTS.Month, startTS.Day, startTS.Hour, startTS.Minute, startTS.Second, 0, startTS.Offset),
                             aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
                else
                {
                    Util.ValidateNumericAggregateValues(aggregate, name: "Foo Bar", count: 1, sum: 0, max: 0, min: 0, stdDev: 0);

                    // This might break: Second boundary might be crossed between snapping test and the aggregation timestamps. Try re-running.
                    Assert.AreEqual(
                             new DateTimeOffset(quickPulseCycleStartTS.Year, quickPulseCycleStartTS.Month, quickPulseCycleStartTS.Day, quickPulseCycleStartTS.Hour, quickPulseCycleStartTS.Minute, quickPulseCycleStartTS.Second, 0, quickPulseCycleStartTS.Offset),
                             aggregate.AggregationPeriodStart);

                    Assert.AreEqual(
                                (stepTSRounded - aggregate.AggregationPeriodStart).TotalMilliseconds,
                                aggregate.AggregationPeriodDuration.TotalMilliseconds);
                }
            }

            Util.CompleteDefaultAggregationCycle(manager);
        }
    }
}
