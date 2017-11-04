using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.ApplicationInsights.DataContracts;
using System.Linq;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricTests
    {
        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void Create()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            // ** Validate metricManager parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => InvokeMetricCtor(
                                    metricManager: null,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "  Foo ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);
            }


            // ** Validate metricId parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: null,
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "   ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "  Foo ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual("  Foo ", metric.MetricId);
            }


            // ** Validate dimension1Name parameter:

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "  \t",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement)
            );

            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: " D1  ",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
            }


            // ** Validate dimension2Name parameter:

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "",
                                    configuration: MetricConfigurations.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: " \r\n",
                                    configuration: MetricConfigurations.Measurement)
            );

            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2\t",
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.AreEqual("D2", metric.GetDimensionName(2));
            }
            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: null,
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
            }
            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
            }


            // ** Validate configuration parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: null)
            );

            {
                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfigurations.Measurement);
                Assert.IsNotNull(metric);

                Assert.AreEqual(MetricConfigurations.Measurement, metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Measurement, metric.GetConfiguration());
            }
            {
                IMetricConfiguration customConfig = new SimpleMetricConfiguration(
                                                                    seriesCountLimit: 10,
                                                                    valuesPerDimensionLimit: 10,
                                                                    seriesConfig: new SimpleMetricSeriesConfiguration(
                                                                                                            lifetimeCounter: true,
                                                                                                            restrictToUInt32Values: true));

                Metric metric = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: customConfig);
                Assert.IsNotNull(metric);

                Assert.AreEqual(customConfig, metric.GetConfiguration());
                Assert.AreSame(customConfig, metric.GetConfiguration());
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void MetricId()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual("Foo", metric.MetricId);
            }
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "\t\tFoo Bar \r\nx ",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual("\t\tFoo Bar \r\nx ", metric.MetricId);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void DimensionsCount()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual(0, metric.DimensionsCount);
            }
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "XXX",
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual(1, metric.DimensionsCount);
            }
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "XXX",
                                        dimension2Name: "XXX",
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual(2, metric.DimensionsCount);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void SeriesCount()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual(1, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(1, "DV11", "DV21"));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(2, "DV11", "DV21"));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(3, "DV12", "DV21"));
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(4, "DV13", "DV21"));
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(5, "DV14", "DV21"));
                Assert.AreEqual(5, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(6, "DV12", "DV22"));
                Assert.AreEqual(6, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(7, "DV12", "DV23"));
                Assert.AreEqual(7, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(8, "DV12", "DV23"));
                Assert.AreEqual(7, metric.SeriesCount);
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                            5,
                                                            1000,
                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));

                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: config);

                Assert.AreEqual(1, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(1, "DV11", "DV21"));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(2, "DV11", "DV21"));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(3, "DV12", "DV21"));
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(4, "DV13", "DV21"));
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(5, "DV14", "DV21"));
                Assert.AreEqual(5, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(6, "DV12", "DV22"));
                Assert.AreEqual(5, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(7, "DV12", "DV23"));
                Assert.AreEqual(5, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(8, "DV12", "DV23"));
                Assert.AreEqual(5, metric.SeriesCount);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetDimensionName()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(3) );
            }
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(0) );
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(3) );
            }
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfigurations.Measurement);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(0) );
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.AreEqual("D2", metric.GetDimensionName(2));
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(3) );
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetDimensionValues()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfigurations.Counter);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );

                IReadOnlyCollection<string> dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(0, dimVals.Count);

                dimVals = metric.GetDimensionValues(2);
                Assert.AreEqual(0, dimVals.Count);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );


                metric.TrackValue(1);
                metric.TrackValue(2);
                metric.TryTrackValue(3, "A", "B");
                metric.TryTrackValue(4, "a", "B");
                metric.TryTrackValue(5, "X", "B");
                metric.TryTrackValue(6, "Y", "B");
                metric.TryTrackValue(7, "Y", "C");
                metric.TryTrackValue(8, "A", "B");

                dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(4, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("A"));
                Assert.IsTrue(dimVals.Contains("a"));
                Assert.IsTrue(dimVals.Contains("X"));
                Assert.IsTrue(dimVals.Contains("Y"));
                Assert.IsFalse(dimVals.Contains("y"));

                dimVals = metric.GetDimensionValues(2);
                Assert.AreEqual(2, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("B"));
                Assert.IsTrue(dimVals.Contains("C"));
                Assert.IsFalse(dimVals.Contains("b"));
                Assert.IsFalse(dimVals.Contains("c"));

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );
            }
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Counter);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );

                IReadOnlyCollection<string> dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(0, dimVals.Count);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );


                metric.TrackValue(1);
                metric.TrackValue(2);
                metric.TryTrackValue(3, "A");
                metric.TryTrackValue(4, "a");
                metric.TryTrackValue(5, "X");
                metric.TryTrackValue(6, "Y");
                metric.TryTrackValue(7, "Y");
                metric.TryTrackValue(8, "A");

                dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(4, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("A"));
                Assert.IsTrue(dimVals.Contains("a"));
                Assert.IsTrue(dimVals.Contains("X"));
                Assert.IsTrue(dimVals.Contains("Y"));
                Assert.IsFalse(dimVals.Contains("y"));

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                            4,
                                                            1000,
                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: config);

                metric.TrackValue(1);
                metric.TrackValue(1);
                Assert.IsTrue(metric.TryTrackValue(2, "A", "B"));
                Assert.IsTrue(metric.TryTrackValue(3, "a", "B"));
                Assert.IsTrue(metric.TryTrackValue(4, "X", "B"));
                Assert.IsFalse(metric.TryTrackValue(5, "Y", "B"));
                Assert.IsFalse(metric.TryTrackValue(5, "Y", "C"));
                Assert.IsTrue(metric.TryTrackValue(5, "A", "B"));

                IReadOnlyCollection<string> dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(3, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("A"));
                Assert.IsTrue(dimVals.Contains("a"));
                Assert.IsTrue(dimVals.Contains("X"));
                Assert.IsFalse(dimVals.Contains("Y"));

                dimVals = metric.GetDimensionValues(2);
                Assert.AreEqual(1, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("B"));

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                            4,
                                                            1000,
                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: null,
                                        configuration: config);

                metric.TrackValue(1);
                metric.TrackValue(1);
                Assert.IsTrue(metric.TryTrackValue(2, "A"));
                Assert.IsTrue(metric.TryTrackValue(3, "a"));
                Assert.IsTrue(metric.TryTrackValue(4, "X"));
                Assert.IsFalse(metric.TryTrackValue(5, "Y"));
                Assert.IsFalse(metric.TryTrackValue(5, "Y"));
                Assert.IsTrue(metric.TryTrackValue(5, "A"));

                IReadOnlyCollection<string> dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(3, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("A"));
                Assert.IsTrue(dimVals.Contains("a"));
                Assert.IsTrue(dimVals.Contains("X"));
                Assert.IsFalse(dimVals.Contains("Y"));

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                            1000,
                                                            2,
                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: config);

                metric.TrackValue(0);
                metric.TrackValue(0);
                Assert.IsTrue(metric.TryTrackValue(0, "A", "B"));
                Assert.IsTrue(metric.TryTrackValue(0, "a", "B"));
                Assert.IsFalse(metric.TryTrackValue(0, "X", "B"));
                Assert.IsFalse(metric.TryTrackValue(0, "Y", "B"));
                Assert.IsFalse(metric.TryTrackValue(0, "Y", "C"));
                Assert.IsTrue(metric.TryTrackValue(0, "A", "B"));
                Assert.IsTrue(metric.TryTrackValue(0, "A", "C"));
                Assert.IsFalse(metric.TryTrackValue(0, "A", "D"));

                IReadOnlyCollection<string> dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(2, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("A"));
                Assert.IsTrue(dimVals.Contains("a"));

                dimVals = metric.GetDimensionValues(2);
                Assert.AreEqual(2, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("B"));
                Assert.IsTrue(dimVals.Contains("C"));

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                            1000,
                                                            2,
                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: null,
                                        configuration: config);

                metric.TrackValue(0);
                metric.TrackValue(0);
                Assert.IsTrue(metric.TryTrackValue(0, "A"));
                Assert.IsTrue(metric.TryTrackValue(0, "a"));
                Assert.IsFalse(metric.TryTrackValue(0, "X"));
                Assert.IsFalse(metric.TryTrackValue(0, "Y"));
                Assert.IsTrue(metric.TryTrackValue(0, "A"));

                IReadOnlyCollection<string> dimVals = metric.GetDimensionValues(1);
                Assert.AreEqual(2, dimVals.Count);
                Assert.IsTrue(dimVals.Contains("A"));
                Assert.IsTrue(dimVals.Contains("a"));

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionValues(3) );
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetAllSeries()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                            5,
                                                            1000,
                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false));

                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: config);

                IReadOnlyList<KeyValuePair<string[], MetricSeries>> series = metric.GetAllSeries();
                Assert.AreEqual(1, series.Count);
                AssertSeries(
                            series[0],
                            expectedKeys:                       new string[0],
                            expectedMetricId:                   "Foo",
                            expectedDimensionNamesAndValues:    new Dictionary<string, string> { },
                            expectedCount:                      null,
                            expectedSum:                        null);


                metric.TrackValue(1);
                metric.TrackValue(2);
                Assert.IsTrue( metric.TryTrackValue(3, "A", "B") );
                Assert.IsTrue( metric.TryTrackValue(4, "a", "B") );
                Assert.IsTrue( metric.TryTrackValue(7, "Y", "C") );
                Assert.IsTrue( metric.TryTrackValue(5, "X", "B") );
                Assert.IsFalse( metric.TryTrackValue(6, "Y", "B") );
                Assert.IsTrue( metric.TryTrackValue(8, "A", "B") );

                series = metric.GetAllSeries();
                Assert.AreEqual(5, series.Count);

                KeyValuePair<string[], MetricSeries>[] sortedSeries = series.OrderBy( (s) => String.Concat(s.Key) ).ToArray();

                AssertSeries(
                            sortedSeries[0],
                            expectedKeys:                       new string[0],
                            expectedMetricId:                   "Foo",
                            expectedDimensionNamesAndValues:    new Dictionary<string, string> { },
                            expectedCount:                      2,
                            expectedSum:                        3);

                 AssertSeries(
                            sortedSeries[1],
                            expectedKeys:                       new string[] { "a", "B" },
                            expectedMetricId:                   "Foo",
                            expectedDimensionNamesAndValues:    new Dictionary<string, string> { ["D1"] = "a", ["D2"] = "B" },
                            expectedCount:                      1,
                            expectedSum:                        4);

                AssertSeries(
                            sortedSeries[2],
                            expectedKeys:                       new string[] { "A", "B" },
                            expectedMetricId:                   "Foo",
                            expectedDimensionNamesAndValues:    new Dictionary<string, string> { ["D1"] = "A", ["D2"] = "B" },
                            expectedCount:                      2,
                            expectedSum:                        11);

                AssertSeries(
                            sortedSeries[3],
                            expectedKeys:                       new string[] { "X", "B" },
                            expectedMetricId:                   "Foo",
                            expectedDimensionNamesAndValues:    new Dictionary<string, string> { ["D1"] = "X", ["D2"] = "B" },
                            expectedCount:                      1,
                            expectedSum:                        5);

                AssertSeries(
                            sortedSeries[4],
                            expectedKeys:                       new string[] { "Y", "C" },
                            expectedMetricId:                   "Foo",
                            expectedDimensionNamesAndValues:    new Dictionary<string, string> { ["D1"] = "Y", ["D2"] = "C" },
                            expectedCount:                      1,
                            expectedSum:                        7);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        private static void AssertSeries(
                                    KeyValuePair<string[], MetricSeries> series,
                                    string[] expectedKeys,
                                    string expectedMetricId,
                                    IDictionary<string, string> expectedDimensionNamesAndValues,
                                    int? expectedCount,
                                    double? expectedSum)
        {
            Assert.IsNotNull(series.Key);

            Assert.AreEqual(expectedKeys.Length, series.Key.Length);
            for (int i = 0; i < expectedKeys.Length; i++)
            {
                Assert.AreEqual(expectedKeys[i], series.Key[i]);
            }
            
            Assert.AreEqual(expectedMetricId, series.Value.MetricId);

            Assert.AreEqual(expectedDimensionNamesAndValues.Count, series.Value.DimensionNamesAndValues.Count);
            foreach (KeyValuePair<string, string> dimNameValue in expectedDimensionNamesAndValues)
            {
                Assert.AreEqual(dimNameValue.Value, series.Value.DimensionNamesAndValues[dimNameValue.Key]);
            }

            MetricAggregate currentAggregate = series.Value.GetCurrentAggregateUnsafe();

            Assert.AreEqual(expectedCount, series.Value.GetCurrentAggregateUnsafe()?.AggregateData?["Count"]);
            Assert.AreEqual(expectedSum, series.Value.GetCurrentAggregateUnsafe()?.AggregateData?["Sum"]);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TryGetDataSeries_CreatingSeriesCorrectly()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                MetricSeries series;
                bool success;

                success = metric.TryGetDataSeries(out series);

                Assert.IsTrue(success);
                Assert.IsNotNull(series);
                Assert.AreEqual("Foo", series.MetricId);

                Assert.AreEqual(0, telemetryCollector.Count);

                series.TrackValue(1);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);
                Assert.AreEqual(1, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(1.0, telemetryCollector[0].AggregateData["Sum"]);
                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(0, telemetryCollector[0].Dimensions.Count);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", createIfNotExists: false) );

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", "Dim2Value") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", "Dim2Value", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", "Dim2Value", createIfNotExists: false) );
            }
            {
                telemetryCollector.Clear();
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "Bar",
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                MetricSeries series;
                bool success;

                success = metric.TryGetDataSeries(out series);

                Assert.IsTrue(success);
                Assert.IsNotNull(series);
                Assert.AreEqual("Foo", series.MetricId);

                Assert.AreEqual(0, telemetryCollector.Count);

                series.TrackValue(2);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);
                Assert.AreEqual(1, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(2.0, telemetryCollector[0].AggregateData["Sum"]);
                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(0, telemetryCollector[0].Dimensions.Count);

                telemetryCollector.Clear();
                MetricSeries series1, series2, series3, series4;

                success = metric.TryGetDataSeries(out series1, "Dim1Value", createIfNotExists: false);
                Assert.IsFalse(success);
                Assert.IsNull(series1);

                success = metric.TryGetDataSeries(out series1, "Dim1Value", createIfNotExists: true);
                Assert.IsTrue(success);
                Assert.IsNotNull(series1);

                success = metric.TryGetDataSeries(out series2, "Dim1Value", createIfNotExists: true);
                Assert.IsTrue(success);
                Assert.IsNotNull(series2);

                success = metric.TryGetDataSeries(out series3, "Dim1Value");
                Assert.IsTrue(success);
                Assert.IsNotNull(series3);

                success = metric.TryGetDataSeries(out series4, "Dim1ValueX");
                Assert.IsTrue(success);
                Assert.IsNotNull(series4);

                Assert.AreEqual(series1, series2);
                Assert.AreEqual(series1, series3);
                Assert.AreEqual(series2, series3);
                Assert.AreNotEqual(series1, series4);

                Assert.AreSame(series1, series2);
                Assert.AreSame(series1, series3);
                Assert.AreSame(series2, series3);
                Assert.AreNotSame(series1, series4);

                Assert.AreEqual(0, telemetryCollector.Count);

                series4.TrackValue(40);
                series3.TrackValue(30);
                series2.TrackValue(20);
                series1.TrackValue(10);
                metricManager.Flush();

                Assert.AreEqual(2, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);
                Assert.IsNotNull(telemetryCollector[1]);

                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(3, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(60.0, telemetryCollector[0].AggregateData["Sum"]);

                Assert.AreEqual(1, telemetryCollector[0].Dimensions.Count);
                Assert.IsTrue(telemetryCollector[0].Dimensions.ContainsKey("Bar"));
                Assert.AreEqual("Dim1Value", telemetryCollector[0].Dimensions["Bar"]);

                Assert.AreEqual("Foo", telemetryCollector[1].MetricId);
                Assert.AreEqual(1, telemetryCollector[1].AggregateData["Count"]);
                Assert.AreEqual(40.0, telemetryCollector[1].AggregateData["Sum"]);

                Assert.AreEqual(1, telemetryCollector[1].Dimensions.Count);
                Assert.IsTrue(telemetryCollector[1].Dimensions.ContainsKey("Bar"));
                Assert.AreEqual("Dim1ValueX", telemetryCollector[1].Dimensions["Bar"]);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1", "Dim2") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1", "Dim2", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1", "Dim2", createIfNotExists: false) );
            }
            {
                telemetryCollector.Clear();
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "Bar",
                                        dimension2Name: "Poo",
                                        configuration: MetricConfigurations.Measurement);

                MetricSeries series;
                bool success;

                success = metric.TryGetDataSeries(out series);

                Assert.IsTrue(success);
                Assert.IsNotNull(series);
                Assert.AreEqual("Foo", series.MetricId);

                Assert.AreEqual(0, telemetryCollector.Count);

                series.TrackValue(2);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);
                Assert.AreEqual(1, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(2.0, telemetryCollector[0].AggregateData["Sum"]);
                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(0, telemetryCollector[0].Dimensions.Count);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", createIfNotExists: false) );

                telemetryCollector.Clear();
                MetricSeries series1, series2, series3, series4, series5, series6;

                success = metric.TryGetDataSeries(out series1, "Dim1Value", "Dim2Value", createIfNotExists: false);
                Assert.IsFalse(success);
                Assert.IsNull(series1);

                success = metric.TryGetDataSeries(out series1, "Dim1Value", "Dim2Value", createIfNotExists: true);
                Assert.IsTrue(success);
                Assert.IsNotNull(series1);

                success = metric.TryGetDataSeries(out series2, "Dim1Value", "Dim2Value", createIfNotExists: true);
                Assert.IsTrue(success);
                Assert.IsNotNull(series2);

                success = metric.TryGetDataSeries(out series3, "Dim1Value", "Dim2Value");
                Assert.IsTrue(success);
                Assert.IsNotNull(series3);

                success = metric.TryGetDataSeries(out series4, "Dim1ValueX", "Dim2ValueX");
                Assert.IsTrue(success);
                Assert.IsNotNull(series4);

                success = metric.TryGetDataSeries(out series5, "Dim1ValueX", "Dim2Value");
                Assert.IsTrue(success);
                Assert.IsNotNull(series5);

                success = metric.TryGetDataSeries(out series6, "Dim1Value", "Dim2ValueX");
                Assert.IsTrue(success);
                Assert.IsNotNull(series6);

                Assert.AreEqual(series1, series2);
                Assert.AreEqual(series1, series3);
                Assert.AreEqual(series2, series3);
                Assert.AreNotEqual(series1, series4);
                Assert.AreNotEqual(series1, series5);
                Assert.AreNotEqual(series1, series6);
                Assert.AreNotEqual(series4, series5);
                Assert.AreNotEqual(series4, series6);
                Assert.AreNotEqual(series5, series6);

                Assert.AreSame(series1, series2);
                Assert.AreSame(series1, series3);
                Assert.AreSame(series2, series3);
                Assert.AreNotSame(series1, series4);
                Assert.AreNotSame(series1, series5);
                Assert.AreNotSame(series1, series6);
                Assert.AreNotSame(series4, series5);
                Assert.AreNotSame(series4, series6);
                Assert.AreNotSame(series5, series6);

                Assert.AreEqual(0, telemetryCollector.Count);

                series6.TrackValue(60);
                series5.TrackValue(50);
                series4.TrackValue(40);
                series3.TrackValue(30);
                series2.TrackValue(20);
                series1.TrackValue(10);
                metricManager.Flush();

                Assert.AreEqual(4, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);
                Assert.IsNotNull(telemetryCollector[1]);
                Assert.IsNotNull(telemetryCollector[2]);
                Assert.IsNotNull(telemetryCollector[3]);

                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(3, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(60.0, telemetryCollector[0].AggregateData["Sum"]);
                
                Assert.AreEqual(2, telemetryCollector[0].Dimensions.Count);
                Assert.IsTrue(telemetryCollector[0].Dimensions.ContainsKey("Bar"));
                Assert.AreEqual("Dim1Value", telemetryCollector[0].Dimensions["Bar"]);
                Assert.AreEqual("Dim2Value", telemetryCollector[0].Dimensions["Poo"]);

                Assert.AreEqual("Foo", telemetryCollector[1].MetricId);
                Assert.AreEqual(1, telemetryCollector[1].AggregateData["Count"]);
                Assert.AreEqual(40.0, telemetryCollector[1].AggregateData["Sum"]);

                Assert.AreEqual(2, telemetryCollector[1].Dimensions.Count);
                Assert.IsTrue(telemetryCollector[1].Dimensions.ContainsKey("Bar"));
                Assert.AreEqual("Dim1ValueX", telemetryCollector[1].Dimensions["Bar"]);
                Assert.AreEqual("Dim2ValueX", telemetryCollector[1].Dimensions["Poo"]);

                Assert.AreEqual("Foo", telemetryCollector[2].MetricId);
                Assert.AreEqual(1, telemetryCollector[2].AggregateData["Count"]);
                Assert.AreEqual(50.0, telemetryCollector[2].AggregateData["Sum"]);

                Assert.AreEqual(2, telemetryCollector[2].Dimensions.Count);
                Assert.IsTrue(telemetryCollector[2].Dimensions.ContainsKey("Bar"));
                Assert.AreEqual("Dim1ValueX", telemetryCollector[2].Dimensions["Bar"]);
                Assert.AreEqual("Dim2Value", telemetryCollector[2].Dimensions["Poo"]);

                Assert.AreEqual("Foo", telemetryCollector[3].MetricId);
                Assert.AreEqual(1, telemetryCollector[3].AggregateData["Count"]);
                Assert.AreEqual(60.0, telemetryCollector[3].AggregateData["Sum"]);

                Assert.AreEqual(2, telemetryCollector[3].Dimensions.Count);
                Assert.IsTrue(telemetryCollector[3].Dimensions.ContainsKey("Bar"));
                Assert.AreEqual("Dim1Value", telemetryCollector[3].Dimensions["Bar"]);
                Assert.AreEqual("Dim2ValueX", telemetryCollector[3].Dimensions["Poo"]);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TryGetDataSeries_NotCreatingWhenLimitsAreReached()
        {
           // Removed due to duplication. We asserted this in several other tests.
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TrackValue()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfigurations.Measurement);

                metric.TrackValue(42);
                metric.TrackValue(-100);
                metric.TrackValue(Double.NaN);
                metric.TrackValue(0.7);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);

                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(3, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(-57.3, telemetryCollector[0].AggregateData["Sum"]);

                Assert.AreEqual(0, telemetryCollector[0].Dimensions.Count);
            }
            telemetryCollector.Clear();
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfigurations.Measurement);

                metric.TrackValue("42");
                metric.TrackValue("-100");
                metric.TrackValue(null);
                Assert.ThrowsException<ArgumentException>( () => metric.TrackValue("") );
                Assert.ThrowsException<ArgumentException>( () => metric.TrackValue("karramba!") );
                metric.TrackValue("0.7");
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsNotNull(telemetryCollector[0]);

                Assert.AreEqual("Foo", telemetryCollector[0].MetricId);
                Assert.AreEqual(3, telemetryCollector[0].AggregateData["Count"]);
                Assert.AreEqual(-57.3, telemetryCollector[0].AggregateData["Sum"]);

                Assert.AreEqual(0, telemetryCollector[0].Dimensions.Count);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TryTrackValue()
        {
            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                    seriesCountLimit: 10,
                                                    valuesPerDimensionLimit: 2,
                                                    seriesConfig: new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: config);

                Assert.AreEqual(1, metric.SeriesCount);

                metric.TrackValue(42);
                Assert.AreEqual(1, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "A", "X"));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.ThrowsException<ArgumentException>( () => metric.TryTrackValue(42, "A", ""));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.ThrowsException<ArgumentNullException>( () => metric.TryTrackValue(42, "A", null));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "B", "X"));
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(42, "C", "X"), "Values per Dim1 limit reached.");
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(42, "C", "Y"), "Values per Dim1 limit reached.");
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.ThrowsException<ArgumentException>( () => metric.TryTrackValue(42, "C", "") );
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.ThrowsException<ArgumentNullException>( () => metric.TryTrackValue(42, "C", null) );
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "A", "Y"));
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "B", "Y"));
                Assert.AreEqual(5, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(42, "B", "Z"));
                Assert.AreEqual(5, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(42, "A", "Z"));
                Assert.AreEqual(5, metric.SeriesCount);

                metricManager.Flush();

                Assert.AreEqual(5, telemetryCollector.Count);

                HashSet<string> results = new HashSet<string>();

                for (int i = 0; i < telemetryCollector.Count; i++)
                {
                    Assert.IsNotNull(telemetryCollector[i]);
                    MetricAggregate aggregate = telemetryCollector[i];

                    Assert.AreEqual("Foo", aggregate.MetricId);
                    Assert.AreEqual(1, aggregate.AggregateData["Count"]);
                    Assert.AreEqual(42.0, aggregate.AggregateData["Sum"]);

                    if (0 == aggregate.Dimensions.Count)
                    {
                        results.Add("-");
                    }
                    else if (2 == aggregate.Dimensions.Count)
                    {
                        results.Add($"{aggregate.Dimensions["D1"]}-{aggregate.Dimensions["D2"]}");
                    }
                    else
                    {
                        Assert.Fail($"Unexpected number of dimensions: {aggregate.Dimensions.Count}.");
                    }
                }

                Assert.AreEqual(5, results.Count);
                Assert.IsTrue(results.Contains("-"));
                Assert.IsTrue(results.Contains("A-X"));
                Assert.IsTrue(results.Contains("B-X"));
                Assert.IsTrue(results.Contains("A-Y"));
                Assert.IsTrue(results.Contains("B-Y"));
            }
            telemetryCollector.Clear();
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(
                                                    seriesCountLimit: 4,
                                                    valuesPerDimensionLimit: 25,
                                                    seriesConfig: new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: null,
                                        configuration: config);

                Assert.AreEqual(1, metric.SeriesCount);

                metric.TrackValue(42);
                Assert.AreEqual(1, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "A"));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.ThrowsException<ArgumentException>(() => metric.TryTrackValue(42, ""));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.ThrowsException<ArgumentNullException>(() => metric.TryTrackValue(42, null));
                Assert.AreEqual(2, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "B"));
                Assert.AreEqual(3, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "C"));
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "C"));
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(42, "D"));
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsFalse(metric.TryTrackValue(42, "E"));
                Assert.AreEqual(4, metric.SeriesCount);

                metricManager.Flush();

                Assert.AreEqual(4, telemetryCollector.Count);

                HashSet<string> results = new HashSet<string>();

                for (int i = 0; i < telemetryCollector.Count; i++)
                {
                    Assert.IsNotNull(telemetryCollector[i]);
                    MetricAggregate aggregate = telemetryCollector[i];

                    Assert.AreEqual("Foo", aggregate.MetricId);

                    bool isC = false;
                    if (0 == aggregate.Dimensions.Count)
                    {
                        results.Add("-");
                    }
                    else if (1 == aggregate.Dimensions.Count)
                    {
                        string dimVal = aggregate.Dimensions["D1"];
                        isC = "C".Equals(dimVal);
                        results.Add(dimVal);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected number of dimensions: {aggregate.Dimensions.Count}.");
                    }

                    if (! isC)
                    {
                        Assert.AreEqual(1, aggregate.AggregateData["Count"]);
                        Assert.AreEqual(42.0, aggregate.AggregateData["Sum"]);
                    }
                    else
                    {
                        Assert.AreEqual(2, aggregate.AggregateData["Count"]);
                        Assert.AreEqual(84.0, aggregate.AggregateData["Sum"]);
                    }
                }

                Assert.AreEqual(4, results.Count);
                Assert.IsTrue(results.Contains("-"));
                Assert.IsTrue(results.Contains("A"));
                Assert.IsTrue(results.Contains("B"));
                Assert.IsTrue(results.Contains("C"));
            }
            telemetryCollector.Clear();
            {
                Metric metric = InvokeMetricCtor(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfigurations.Measurement);

                Assert.AreEqual(1, metric.SeriesCount);

                metric.TrackValue(42);
                Assert.AreEqual(1, metric.SeriesCount);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryTrackValue(42, "A") );
                Assert.AreEqual(1, metric.SeriesCount);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryTrackValue(42, "A", "X") );
                Assert.AreEqual(1, metric.SeriesCount);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TestEquals()
        {
            MetricManager metricManager = new MetricManager(new MemoryMetricTelemetryPipeline());
            Metric metric1 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.IsFalse(metric1.Equals(null));
            Assert.IsFalse(metric1.Equals("some object"));
            Assert.IsTrue(metric1.Equals(metric1));

            Metric metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.IsTrue(metric1.Equals(metric2));
            Assert.IsTrue(metric2.Equals(metric1));

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2x",
                                    configuration: MetricConfigurations.Counter);

            Assert.IsFalse(metric1.Equals(metric2));
            Assert.IsFalse(metric2.Equals(metric1));

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: null,
                                    configuration: MetricConfigurations.Counter);

            Assert.IsFalse(metric1.Equals(metric2));
            Assert.IsFalse(metric2.Equals(metric1));

            metric2 = InvokeMetricCtor(
                                   metricManager,
                                   metricId: "Foo",
                                   dimension1Name: null,
                                   dimension2Name: null,
                                   configuration: MetricConfigurations.Counter);

            Assert.IsFalse(metric1.Equals(metric2));
            Assert.IsFalse(metric2.Equals(metric1));

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1x",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.IsFalse(metric1.Equals(metric2));
            Assert.IsFalse(metric2.Equals(metric1));

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foox",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.IsFalse(metric1.Equals(metric2));
            Assert.IsFalse(metric2.Equals(metric1));

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement);

            Assert.IsTrue(metric1.Equals(metric2));
            Assert.IsTrue(metric2.Equals(metric1));

            MetricManager anotherMetricManager = TelemetryConfiguration.Active.Metrics();
            metric2 = InvokeMetricCtor(
                                    anotherMetricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.IsTrue(metric1.Equals(metric2));
            Assert.IsTrue(metric2.Equals(metric1));

            Util.CompleteDefaultAggregationCycle(anotherMetricManager);

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TestGetHashCode()
        {
            MetricManager metricManager = new MetricManager(new MemoryMetricTelemetryPipeline());
            Metric metric1 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreEqual(metric1.GetHashCode(), metric1.GetHashCode());

            Metric metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.AreEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2x",
                                    configuration: MetricConfigurations.Counter);

            Assert.AreNotEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: null,
                                    configuration: MetricConfigurations.Counter);

            Assert.AreNotEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfigurations.Counter);

            Assert.AreNotEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1x",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.AreNotEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foox",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.AreNotEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            metric2 = InvokeMetricCtor(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Measurement);

            Assert.AreEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            MetricManager anotherMetricManager = TelemetryConfiguration.Active.Metrics();
            metric2 = InvokeMetricCtor(
                                    anotherMetricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfigurations.Counter);

            Assert.AreEqual(metric1.GetHashCode(), metric2.GetHashCode());
            Assert.AreNotEqual(0, metric1.GetHashCode());
            Assert.AreNotEqual(0, metric2.GetHashCode());

            Util.CompleteDefaultAggregationCycle(anotherMetricManager);

            Util.CompleteDefaultAggregationCycle(metricManager);
        }


        private static Metric InvokeMetricCtor(MetricManager metricManager, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration configuration)
        {
            // Metric ctor is private..

            Type apiType = typeof(Metric);
            const string apiName = "TelemetryConfiguration";

            ConstructorInfo ctor = apiType.GetTypeInfo().GetConstructor(
                                                                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                                                                    binder: null,
                                                                    types: new Type[] { typeof(MetricManager),
                                                                                        typeof(string),
                                                                                        typeof(string),
                                                                                        typeof(string),
                                                                                        typeof(IMetricConfiguration)},
                                                                    modifiers: null);

            if (ctor == null)
            {
                throw new InvalidOperationException($"Could not get ConstructorInfo for {apiType.Name}.{apiName} via reflection."
                                                   + " This is either an internal SDK bug or there is a mismatch between the Metrics-SDK version"
                                                   + " and the Application Insights Base SDK version. Please report this issue.");
            }

            try
            {
                object metricObject = ctor.Invoke(new object[] { metricManager, metricId, dimension1Name, dimension2Name, configuration });

                Assert.IsNotNull(metricObject);
                Assert.IsInstanceOfType(metricObject, typeof(Metric));

                return (Metric) metricObject;
            }
            catch(TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                }
                else
                {
                    ExceptionDispatchInfo.Capture(tie).Throw();
                }

                return null; // Never reached.
            }
        }
    }
}
