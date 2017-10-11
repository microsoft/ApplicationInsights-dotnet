using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics;
//using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using Microsoft.ApplicationInsights.Channel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.ApplicationInsights.DataContracts;

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
                    () => CreateMetric(
                                    metricManager: null,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "  Foo ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
            }


            // ** Validate metricId parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: null,
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "   ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "  Foo ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual("Foo", metric.MetricId);
            }


            // ** Validate dimension1Name parameter:

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "  \t",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: " D1  ",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
            }


            // ** Validate dimension2Name parameter:

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: " \r\n",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2\t",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.AreEqual("D2", metric.GetDimensionName(2));
            }
            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: null,
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
            }
            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
            }


            // ** Validate configuration parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: null)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);

                Assert.AreEqual(MetricConfiguration.Measurement, metric.GetConfiguration());
                Assert.AreSame(MetricConfiguration.Measurement, metric.GetConfiguration());
            }
            {
                IMetricConfiguration customConfig = new SimpleMetricConfiguration(
                                                                    seriesCountLimit: 10,
                                                                    valuesPerDimensionLimit: 10,
                                                                    seriesConfig: new SimpleMetricSeriesConfiguration(
                                                                                                            lifetimeCounter: true,
                                                                                                            restrictToUInt32Values: true));

                Metric metric = CreateMetric(
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
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                Assert.AreEqual("Foo", metric.MetricId);
            }
            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "\t\tFoo Bar \r\nx ",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                Assert.AreEqual("Foo Bar \r\nx", metric.MetricId);
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
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                Assert.AreEqual(0, metric.DimensionsCount);
            }
            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "XXX",
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                Assert.AreEqual(1, metric.DimensionsCount);
            }
            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "XXX",
                                        dimension2Name: "XXX",
                                        configuration: MetricConfiguration.Measurement);

                Assert.AreEqual(2, metric.DimensionsCount);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void SeriesCount()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfiguration.Measurement);

                Assert.AreEqual(1, metric.SeriesCount);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);

            //throw new NotImplementedException();
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetDimensionName()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(0) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(3) );
            }
            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(-1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(0) );
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(3) );
            }
            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfiguration.Measurement);

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
        public void GetAllSeries()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfiguration.Measurement);

                IReadOnlyList<KeyValuePair<string[], MetricSeries>> series = metric.GetAllSeries();
                Assert.IsNotNull(series);
                Assert.AreEqual(1, series.Count);

                Assert.IsNotNull(series[0].Key);
                Assert.AreEqual(0, series[0].Key.Length);

                Assert.IsNotNull(series[0].Value);
                Assert.AreEqual("Foo", series[0].Value.MetricId);
                Assert.AreEqual(0, series[0].Value.Context?.Properties?.Count);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);

            //throw new NotImplementedException();
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TryGetDataSeries_CreatingSeriesCorrectly()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: null,
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                MetricSeries series;
                bool success;

                success = metric.TryGetDataSeries(out series);

                Assert.IsTrue(success);
                Assert.IsNotNull(series);
                Assert.AreEqual("Foo", series.MetricId);
                Assert.AreEqual(0, series.Context?.Properties?.Count);

                Assert.AreEqual(0, telemetryCollector.Count);

                series.TrackValue(1);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Sum);
                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", createIfNotExists: false) );

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", "Dim2Value") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", "Dim2Value", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1Value", "Dim2Value", createIfNotExists: false) );
            }
            {
                telemetryCollector.Clear();
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "Bar",
                                        dimension2Name: null,
                                        configuration: MetricConfiguration.Measurement);

                MetricSeries series;
                bool success;

                success = metric.TryGetDataSeries(out series);

                Assert.IsTrue(success);
                Assert.IsNotNull(series);
                Assert.AreEqual("Foo", series.MetricId);
                Assert.AreEqual(0, series.Context?.Properties?.Count);

                Assert.AreEqual(0, telemetryCollector.Count);

                series.TrackValue(2);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(2, ((MetricTelemetry) telemetryCollector[0]).Sum);
                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);

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
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));
                Assert.IsInstanceOfType(telemetryCollector[1], typeof(MetricTelemetry));

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(60, ((MetricTelemetry) telemetryCollector[0]).Sum);
                
                Assert.AreEqual(2, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey("Bar") ?? false);
                Assert.AreEqual("Dim1Value", ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?["Bar"]);

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[1]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[1]).Count);
                Assert.AreEqual(40, ((MetricTelemetry) telemetryCollector[1]).Sum);

                Assert.AreEqual(2, ((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.ContainsKey("Bar") ?? false);
                Assert.AreEqual("Dim1ValueX", ((MetricTelemetry) telemetryCollector[1]).Context?.Properties?["Bar"]);

                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1", "Dim2") );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1", "Dim2", createIfNotExists: true) );
                Assert.ThrowsException<InvalidOperationException>( () => metric.TryGetDataSeries(out series, "Dim1", "Dim2", createIfNotExists: false) );
            }
            {
                telemetryCollector.Clear();
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "Bar",
                                        dimension2Name: "Poo",
                                        configuration: MetricConfiguration.Measurement);

                MetricSeries series;
                bool success;

                success = metric.TryGetDataSeries(out series);

                Assert.IsTrue(success);
                Assert.IsNotNull(series);
                Assert.AreEqual("Foo", series.MetricId);
                Assert.AreEqual(0, series.Context?.Properties?.Count);

                Assert.AreEqual(0, telemetryCollector.Count);

                series.TrackValue(2);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(2, ((MetricTelemetry) telemetryCollector[0]).Sum);
                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);

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
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));
                Assert.IsInstanceOfType(telemetryCollector[1], typeof(MetricTelemetry));
                Assert.IsInstanceOfType(telemetryCollector[2], typeof(MetricTelemetry));
                Assert.IsInstanceOfType(telemetryCollector[3], typeof(MetricTelemetry));

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(60, ((MetricTelemetry) telemetryCollector[0]).Sum);
                
                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey("Bar") ?? false);
                Assert.AreEqual("Dim1Value", ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?["Bar"]);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey("Poo") ?? false);
                Assert.AreEqual("Dim2Value", ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?["Poo"]);

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[1]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[1]).Count);
                Assert.AreEqual(40, ((MetricTelemetry) telemetryCollector[1]).Sum);

                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.ContainsKey("Bar") ?? false);
                Assert.AreEqual("Dim1ValueX", ((MetricTelemetry) telemetryCollector[1]).Context?.Properties?["Bar"]);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[1]).Context?.Properties?.ContainsKey("Poo") ?? false);
                Assert.AreEqual("Dim2ValueX", ((MetricTelemetry) telemetryCollector[1]).Context?.Properties?["Poo"]);

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[2]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[2]).Count);
                Assert.AreEqual(50, ((MetricTelemetry) telemetryCollector[2]).Sum);

                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[2]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[2]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[2]).Context?.Properties?.ContainsKey("Bar") ?? false);
                Assert.AreEqual("Dim1ValueX", ((MetricTelemetry) telemetryCollector[2]).Context?.Properties?["Bar"]);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[2]).Context?.Properties?.ContainsKey("Poo") ?? false);
                Assert.AreEqual("Dim2Value", ((MetricTelemetry) telemetryCollector[2]).Context?.Properties?["Poo"]);

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[3]).Name);
                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[3]).Count);
                Assert.AreEqual(60, ((MetricTelemetry) telemetryCollector[3]).Sum);

                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[3]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[3]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[3]).Context?.Properties?.ContainsKey("Bar") ?? false);
                Assert.AreEqual("Dim1Value", ((MetricTelemetry) telemetryCollector[3]).Context?.Properties?["Bar"]);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[3]).Context?.Properties?.ContainsKey("Poo") ?? false);
                Assert.AreEqual("Dim2ValueX", ((MetricTelemetry) telemetryCollector[3]).Context?.Properties?["Poo"]);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TryGetDataSeries_NotCreatingWhenLimitsAreReached()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfiguration.Measurement);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TrackValue()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfiguration.Measurement);

                metric.TrackValue(42);
                metric.TrackValue(-100);
                metric.TrackValue(Double.NaN);
                metric.TrackValue(0.7);
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(-57.3, ((MetricTelemetry) telemetryCollector[0]).Sum);

                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
            }
            telemetryCollector.Clear();
            {
                Metric metric = CreateMetric(
                                        metricManager,
                                        metricId: "Foo",
                                        dimension1Name: "D1",
                                        dimension2Name: "D2",
                                        configuration: MetricConfiguration.Measurement);

                metric.TrackValue("42");
                metric.TrackValue("-100");
                metric.TrackValue(null);
                Assert.ThrowsException<ArgumentException>( () => metric.TrackValue("") );
                Assert.ThrowsException<ArgumentException>( () => metric.TrackValue("karramba!") );
                metric.TrackValue("0.7");
                metricManager.Flush();

                Assert.AreEqual(1, telemetryCollector.Count);
                Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));

                Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[0]).Count);
                Assert.AreEqual(-57.3, ((MetricTelemetry) telemetryCollector[0]).Sum);

                Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
            }

            Util.CompleteDefaultAggregationCycle(metricManager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void TryTrackValue()
        {
            // The Metric ctor is internal, we use reflection to invoke it.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);
            IMetricConfiguration config = new SimpleMetricConfiguration(
                                                    seriesCountLimit: 10,
                                                    valuesPerDimensionLimit: 2,
                                                    seriesConfig: new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false));

            {
                Metric metric = CreateMetric(
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

                Assert.IsFalse(metric.TryTrackValue(42, "A", "Z"), "Values per Dim2 for Dim1=A limit reached.");
                Assert.AreEqual(4, metric.SeriesCount);

                Assert.IsTrue(metric.TryTrackValue(42, "B", "Z"), "Values per Dim2 for Dim1=B limit was not yet reached.");
                Assert.AreEqual(5, metric.SeriesCount);


                //metricManager.Flush();

                //Assert.AreEqual(1, telemetryCollector.Count);
                //Assert.IsInstanceOfType(telemetryCollector[0], typeof(MetricTelemetry));

                //Assert.AreEqual("Foo", ((MetricTelemetry) telemetryCollector[0]).Name);
                //Assert.AreEqual(3, ((MetricTelemetry) telemetryCollector[0]).Count);
                //Assert.AreEqual(-57.3, ((MetricTelemetry) telemetryCollector[0]).Sum);

                //Assert.AreEqual(1, ((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.Count);
                //Assert.IsTrue(((MetricTelemetry) telemetryCollector[0]).Context?.Properties?.ContainsKey(Util.AggregationIntervalMonikerPropertyKey) ?? false);
            }
            telemetryCollector.Clear();
            

            Util.CompleteDefaultAggregationCycle(metricManager);
        }


        private static Metric CreateMetric(MetricManager metricManager, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration configuration)
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
