using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics.TestUtil;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Linq;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class TelemetryClientExtensionsTests
    {
        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetMetric_SendsData()
        {
            IList<ITelemetry> sentTelemetry;
            TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out sentTelemetry);
            TelemetryClient client = new TelemetryClient(telemetryPipeline);

            {
                Metric metric = client.GetMetric("CowsSold");
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.AreEqual("CowsSold", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                metric.TrackValue(0.5);
                metric.TrackValue(0.6);
                Assert.ThrowsException<InvalidOperationException>(() => metric.TryTrackValue(1.5, "A"));
                Assert.ThrowsException<InvalidOperationException>(() => metric.TryTrackValue(2.5, "A", "X"));

                telemetryPipeline.GetMetricManager().Flush();
                Assert.AreEqual(1, sentTelemetry.Count);
                Util.ValidateNumericAggregateValues(sentTelemetry[0], "CowsSold", 2, 1.1, 0.6, 0.5, 0.05);
                Assert.AreEqual(1, sentTelemetry[0].Context.Properties.Count);
                Assert.IsTrue(sentTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                sentTelemetry.Clear();

                metric.TrackValue(0.7);
                metric.TrackValue(0.8);

                telemetryPipeline.GetMetricManager().Flush();
                Assert.AreEqual(1, sentTelemetry.Count);
                Util.ValidateNumericAggregateValues(sentTelemetry[0], "CowsSold", 2, 1.5, 0.8, 0.7, 0.05);
                Assert.AreEqual(1, sentTelemetry[0].Context.Properties.Count);
                Assert.IsTrue(sentTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                sentTelemetry.Clear();
            }
            {
                Metric metric = client.GetMetric("CowsSold", "Color", MetricConfigurations.Common.Accumulator());
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("CowsSold", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());

                metric.TryTrackValue(0.5, "Purple");
                metric.TryTrackValue(0.6, "Purple");
                Assert.ThrowsException<InvalidOperationException>(() => metric.TryTrackValue(2.5, "A", "X"));

                telemetryPipeline.GetMetricManager().Flush();
                Assert.AreEqual(1, sentTelemetry.Count);
                Util.ValidateNumericAggregateValues(sentTelemetry[0], "CowsSold", 1, 1.1, 1.1, 0.5, null);
                Assert.AreEqual(2, sentTelemetry[0].Context.Properties.Count);
                Assert.IsTrue(sentTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", sentTelemetry[0].Context.Properties["Color"]);
                sentTelemetry.Clear();

                metric.TryTrackValue(0.7, "Purple");
                metric.TryTrackValue(0.8, "Purple");

                telemetryPipeline.GetMetricManager().Flush();
                Assert.AreEqual(1, sentTelemetry.Count);
                Util.ValidateNumericAggregateValues(sentTelemetry[0], "CowsSold", 1, 2.6, 2.6, 0.5, null);
                Assert.AreEqual(2, sentTelemetry[0].Context.Properties.Count);
                Assert.IsTrue(sentTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", sentTelemetry[0].Context.Properties["Color"]);
                sentTelemetry.Clear();
            }
            {
                Metric metric = client.GetMetric("CowsSold", "Color", "Size", MetricConfigurations.Common.Accumulator());
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("CowsSold", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());

                metric.TryTrackValue(0.5, "Purple", "Large");
                metric.TryTrackValue(0.6, "Purple", "Large");

                telemetryPipeline.GetMetricManager().Flush();
                Assert.AreEqual(2, sentTelemetry.Count);

                MetricTelemetry[] orderedTelemetry = sentTelemetry
                                                        .OrderByDescending((t) => ((MetricTelemetry) t).Count * 10000 + ((MetricTelemetry) t).Sum)
                                                        .Select((t) => (MetricTelemetry) t)
                                                        .ToArray();

                // This one is from the prev section:
                Util.ValidateNumericAggregateValues(orderedTelemetry[0], "CowsSold", 1, 2.6, 2.6, 0.5, null);
                Assert.AreEqual(2, orderedTelemetry[0].Context.Properties.Count);
                Assert.IsTrue(orderedTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", orderedTelemetry[0].Context.Properties["Color"]);

                Util.ValidateNumericAggregateValues(orderedTelemetry[1], "CowsSold", 1, 1.1, 1.1, 0.5, null);
                Assert.AreEqual(3, orderedTelemetry[1].Context.Properties.Count);
                Assert.IsTrue(orderedTelemetry[1].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", orderedTelemetry[1].Context.Properties["Color"]);
                Assert.AreEqual("Large", orderedTelemetry[1].Context.Properties["Size"]);
                sentTelemetry.Clear();

                metric.TryTrackValue(0.7, "Purple", "Large");
                metric.TryTrackValue(0.8, "Purple", "Small");

                telemetryPipeline.GetMetricManager().Flush();
                Assert.AreEqual(3, sentTelemetry.Count);

                orderedTelemetry = sentTelemetry
                                            .OrderByDescending((t) => ((MetricTelemetry) t).Count * 10000 + ((MetricTelemetry) t).Sum)
                                            .Select((t) => (MetricTelemetry) t)
                                            .ToArray();

                // This one is from the prev section:
                Util.ValidateNumericAggregateValues(orderedTelemetry[0], "CowsSold", 1, 2.6, 2.6, 0.5, null);
                Assert.AreEqual(2, orderedTelemetry[0].Context.Properties.Count);
                Assert.IsTrue(orderedTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", orderedTelemetry[0].Context.Properties["Color"]);

                Util.ValidateNumericAggregateValues(orderedTelemetry[1], "CowsSold", 1, 1.8, 1.8, 0.5, null);
                Assert.AreEqual(3, orderedTelemetry[1].Context.Properties.Count);
                Assert.IsTrue(orderedTelemetry[1].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", orderedTelemetry[1].Context.Properties["Color"]);
                Assert.AreEqual("Large", orderedTelemetry[1].Context.Properties["Size"]);

                Util.ValidateNumericAggregateValues(orderedTelemetry[2], "CowsSold", 1, 0.8, 0.8, 0.8, null);
                Assert.AreEqual(3, orderedTelemetry[2].Context.Properties.Count);
                Assert.IsTrue(orderedTelemetry[2].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                Assert.AreEqual("Purple", orderedTelemetry[2].Context.Properties["Color"]);
                Assert.AreEqual("Small", orderedTelemetry[2].Context.Properties["Size"]);

                sentTelemetry.Clear();
            }

            Util.CompleteDefaultAggregationCycle(telemetryPipeline.GetMetricManager());
            telemetryPipeline.Dispose();
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetMetric_RespectsMetricConfiguration()
        {
            IList<ITelemetry> sentTelemetry;
            TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out sentTelemetry);
            TelemetryClient client = new TelemetryClient(telemetryPipeline);

            {
                Metric metric = client.GetMetric("M1");
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.AreEqual("M1", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M2", MetricConfigurations.Common.Measurement());
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.AreEqual("M2", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M3", MetricConfigurations.Common.Accumulator());
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.AreEqual("M3", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(true));
                Metric metric = client.GetMetric("M4", config);
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.AreEqual("M4", metric.MetricId);
                Assert.AreEqual(config, metric.GetConfiguration());
                Assert.AreSame(config, metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(new MetricSeriesConfigurationForMeasurement(true), series.GetConfiguration());
                Assert.AreNotSame(new MetricSeriesConfigurationForMeasurement(true), series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M5", "Dim1");
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("M5", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val"));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M6", "Dim1", MetricConfigurations.Common.Measurement());
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("M6", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val"));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M7", "Dim1", MetricConfigurations.Common.Accumulator());
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("M7", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val"));
                Assert.AreEqual(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(true));
                Metric metric = client.GetMetric("M8", "Dim1", config);
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("M8", metric.MetricId);
                Assert.AreEqual(config, metric.GetConfiguration());
                Assert.AreSame(config, metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(config.SeriesConfig, series.GetConfiguration());
                Assert.AreSame(config.SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val"));
                Assert.AreEqual(new MetricSeriesConfigurationForMeasurement(true), series.GetConfiguration());
                Assert.AreNotSame(new MetricSeriesConfigurationForMeasurement(true), series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M9", "Dim1", "Dim2");
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("Dim2", metric.GetDimensionName(2));
                Assert.AreEqual("M9", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val", "Dim2val"));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M10", "Dim1", "Dim2", MetricConfigurations.Common.Measurement());
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("Dim2", metric.GetDimensionName(2));
                Assert.AreEqual("M10", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val", "Dim2val"));
                Assert.AreEqual(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement().SeriesConfig, series.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("M11", "Dim1", "Dim2", MetricConfigurations.Common.Accumulator());
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("Dim2", metric.GetDimensionName(2));
                Assert.AreEqual("M11", metric.MetricId);
                Assert.AreEqual(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val", "Dim2val"));
                Assert.AreEqual(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator().SeriesConfig, series.GetConfiguration());
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(true));
                Metric metric = client.GetMetric("M12", "Dim1", "Dim2", config);
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("Dim1", metric.GetDimensionName(1));
                Assert.AreEqual("Dim2", metric.GetDimensionName(2));
                Assert.AreEqual("M12", metric.MetricId);
                Assert.AreEqual(config, metric.GetConfiguration());
                Assert.AreSame(config, metric.GetConfiguration());

                MetricSeries series;
                Assert.IsTrue(metric.TryGetDataSeries(out series));
                Assert.AreEqual(config.SeriesConfig, series.GetConfiguration());
                Assert.AreSame(config.SeriesConfig, series.GetConfiguration());
                Assert.IsTrue(metric.TryGetDataSeries(out series, "Dim1Val", "Dim2val"));
                Assert.AreEqual(new MetricSeriesConfigurationForMeasurement(true), series.GetConfiguration());
                Assert.AreNotSame(new MetricSeriesConfigurationForMeasurement(true), series.GetConfiguration());
                Assert.AreSame(config.SeriesConfig, series.GetConfiguration());
            }

            Util.CompleteDefaultAggregationCycle(telemetryPipeline.GetMetricManager());
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetMetric_DetectsMetricConfigurationConflicts()
        {
            IList<ITelemetry> sentTelemetry;
            TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out sentTelemetry);
            TelemetryClient client = new TelemetryClient(telemetryPipeline);

            {
                Metric m1 = client.GetMetric("M01");
                Assert.IsNotNull(m1);

                Metric m2 = client.GetMetric("M01");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M01 ");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M01", MetricConfigurations.Common.Measurement());
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M01", metricConfiguration: null);
                Assert.AreSame(m1, m2);

                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M01", MetricConfigurations.Common.Accumulator()));

                IMetricConfiguration config1 = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(false));
                IMetricConfiguration config2 = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(false));
                Assert.AreEqual(config1, config2);
                Assert.AreNotSame(config1, config2);

                m1 = client.GetMetric("M02", config1);
                Assert.IsNotNull(m1);

                m2 = client.GetMetric("M02", config2);
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M02", metricConfiguration: null);
                Assert.AreSame(m1, m2);

                config2 = new SimpleMetricConfiguration(10, 101, new MetricSeriesConfigurationForMeasurement(false));
                Assert.AreNotEqual(config1, config2);
                Assert.AreNotSame(config1, config2);

                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M02", config2));
                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M02 ", config2));
            }
            {
                Metric m1 = client.GetMetric("M11", "Dim1");
                Assert.IsNotNull(m1);

                Metric m2 = client.GetMetric("M11", "Dim1");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric(" M11", "Dim1");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M11", " Dim1");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric(" M11", " Dim1", MetricConfigurations.Common.Measurement());
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M11", "Dim1", metricConfiguration: null);
                Assert.AreSame(m1, m2);

                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M11", "Dim1 ", MetricConfigurations.Common.Accumulator()));

                IMetricConfiguration config1 = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(false));
                IMetricConfiguration config2 = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(false));
                Assert.AreEqual(config1, config2);
                Assert.AreNotSame(config1, config2);

                m1 = client.GetMetric("M12 ", "Dim1", config1);
                Assert.IsNotNull(m1);

                m2 = client.GetMetric("M12", "Dim1 ", config2);
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M12", "Dim1", metricConfiguration: null);
                Assert.AreSame(m1, m2);

                config2 = new SimpleMetricConfiguration(10, 101, new MetricSeriesConfigurationForMeasurement(false));
                Assert.AreNotEqual(config1, config2);
                Assert.AreNotSame(config1, config2);

                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M12", "Dim1", config2));
                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M12 ", "Dim1", config2));
            }
            {
                Metric m1 = client.GetMetric("M21", "Dim1", "Dim2");
                Assert.IsNotNull(m1);

                Metric m2 = client.GetMetric("M21", "Dim1", "Dim2");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric(" M21", "Dim1", "Dim2");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M21", " Dim1", "Dim2");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M21", "Dim1", " Dim2");
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric(" M21", " Dim1", "Dim2", MetricConfigurations.Common.Measurement());
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M21", "Dim1", "Dim2 ", metricConfiguration: null);
                Assert.AreSame(m1, m2);

                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M21", "Dim1 ", "Dim2", MetricConfigurations.Common.Accumulator()));

                IMetricConfiguration config1 = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(false));
                IMetricConfiguration config2 = new SimpleMetricConfiguration(10, 10, new MetricSeriesConfigurationForMeasurement(false));
                Assert.AreEqual(config1, config2);
                Assert.AreNotSame(config1, config2);

                m1 = client.GetMetric("M22 ", "Dim1", "Dim2 ", config1);
                Assert.IsNotNull(m1);

                m2 = client.GetMetric("M22", "Dim1 ", "Dim2", config2);
                Assert.AreSame(m1, m2);

                m2 = client.GetMetric("M22", "Dim1", "Dim2", metricConfiguration: null);
                Assert.AreSame(m1, m2);

                config2 = new SimpleMetricConfiguration(10, 101, new MetricSeriesConfigurationForMeasurement(false));
                Assert.AreNotEqual(config1, config2);
                Assert.AreNotSame(config1, config2);

                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M22", "Dim1", "Dim2", config2));
                Assert.ThrowsException<ArgumentException>(() => client.GetMetric("M22 ", "Dim1", "Dim2", config2));
            }
            {
                Metric m0 = client.GetMetric("Xxx");
                Metric m1 = client.GetMetric("Xxx", "Dim1");
                Metric m2 = client.GetMetric("Xxx", "Dim1", "Dim2");

                Assert.IsNotNull(m0);
                Assert.IsNotNull(m1);
                Assert.IsNotNull(m2);

                Assert.AreNotSame(m0, m1);
                Assert.AreNotSame(m0, m2);
                Assert.AreNotSame(m1, m2);

                Assert.AreSame(m0.GetConfiguration(), m1.GetConfiguration());
                Assert.AreSame(m0.GetConfiguration(), m2.GetConfiguration());
                Assert.AreSame(m1.GetConfiguration(), m2.GetConfiguration());

                Assert.AreSame(MetricConfigurations.Common.Measurement(), m0.GetConfiguration());
            }

            Util.CompleteDefaultAggregationCycle(telemetryPipeline.GetMetricManager());
            telemetryPipeline.Dispose();
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetMetric_RespectsAggregationScope()
        {
            IList<ITelemetry> sentTelemetry1, sentTelemetry2;
            TelemetryConfiguration telemetryPipeline1 = Util.CreateAITelemetryConfig(out sentTelemetry1);
            TelemetryConfiguration telemetryPipeline2 = Util.CreateAITelemetryConfig(out sentTelemetry2);
            TelemetryClient client11 = new TelemetryClient(telemetryPipeline1);
            TelemetryClient client12 = new TelemetryClient(telemetryPipeline1);
            TelemetryClient client21 = new TelemetryClient(telemetryPipeline2);

            Metric metricA111 = client11.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryConfiguration);
            metricA111.TrackValue(101);
            metricA111.TrackValue(102);
            metricA111.TryTrackValue(111, "Val");
            metricA111.TryTrackValue(112, "Val");

            Metric metricA112 = client11.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement());
            metricA112.TrackValue(103);
            metricA112.TrackValue(104);
            metricA112.TryTrackValue(113, "Val");
            metricA112.TryTrackValue(114, "Val");

            Metric metricA113 = client11.GetMetric("Metric A", "Dim1");
            metricA113.TrackValue(105);
            metricA113.TrackValue(106);
            metricA113.TryTrackValue(115, "Val");
            metricA113.TryTrackValue(116, "Val");

            Assert.AreSame(metricA111, metricA112);
            Assert.AreSame(metricA111, metricA113);
            Assert.AreSame(metricA112, metricA113);

            MetricSeries series1, series2;
            Assert.IsTrue(metricA111.TryGetDataSeries(out series1));
            Assert.IsTrue(metricA112.TryGetDataSeries(out series2));
            Assert.AreSame(series1, series2);
            Assert.IsTrue(metricA113.TryGetDataSeries(out series2));
            Assert.AreSame(series1, series2);
            Assert.IsTrue(metricA112.TryGetDataSeries(out series1));
            Assert.AreSame(series1, series2);

            Assert.IsTrue(metricA111.TryGetDataSeries(out series1, "Val"));
            Assert.IsTrue(metricA112.TryGetDataSeries(out series2, "Val"));
            Assert.AreSame(series1, series2);
            Assert.IsTrue(metricA113.TryGetDataSeries(out series2, "Val"));
            Assert.AreSame(series1, series2);
            Assert.IsTrue(metricA112.TryGetDataSeries(out series1, "Val"));
            Assert.AreSame(series1, series2);

            Metric metricA121 = client12.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryConfiguration);
            metricA121.TrackValue(107);
            metricA121.TrackValue(108);
            metricA121.TryTrackValue(117, "Val");
            metricA121.TryTrackValue(118, "Val");

            Assert.AreSame(metricA111, metricA121);

            Metric metricA211 = client21.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryConfiguration);
            metricA211.TrackValue(201);
            metricA211.TrackValue(202);
            metricA211.TryTrackValue(211, "Val");
            metricA211.TryTrackValue(212, "Val");

            Assert.AreNotSame(metricA111, metricA211);

            Metric metricA11c1 = client11.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient);
            metricA11c1.TrackValue(301);
            metricA11c1.TrackValue(302);
            metricA11c1.TryTrackValue(311, "Val");
            metricA11c1.TryTrackValue(312, "Val");

            Metric metricA11c2 = client11.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient);
            metricA11c2.TrackValue(303);
            metricA11c2.TrackValue(304);
            metricA11c2.TryTrackValue(313, "Val");
            metricA11c2.TryTrackValue(314, "Val");

            Assert.AreNotSame(metricA111, metricA11c1);
            Assert.AreSame(metricA11c1, metricA11c2);

            Assert.IsTrue(metricA11c1.TryGetDataSeries(out series1));
            Assert.IsTrue(metricA11c1.TryGetDataSeries(out series2));
            Assert.AreSame(series1, series2);

            Assert.IsTrue(metricA11c1.TryGetDataSeries(out series1, "Val"));
            Assert.IsTrue(metricA11c1.TryGetDataSeries(out series2, "Val"));
            Assert.AreSame(series1, series2);

            Metric metricA12c1 = client12.GetMetric("Metric A", "Dim1", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient);
            metricA12c1.TrackValue(305);
            metricA12c1.TrackValue(306);
            metricA12c1.TryTrackValue(315, "Val");
            metricA12c1.TryTrackValue(316, "Val");

            Assert.AreNotSame(metricA11c1, metricA12c1);

            client11.GetMetricManager(MetricAggregationScope.TelemetryClient).Flush();
            client12.GetMetricManager(MetricAggregationScope.TelemetryClient).Flush();
            client21.GetMetricManager(MetricAggregationScope.TelemetryClient).Flush();
            telemetryPipeline1.GetMetricManager().Flush();
            telemetryPipeline2.GetMetricManager().Flush();

            Assert.AreEqual(6, sentTelemetry1.Count);
            Assert.AreEqual(2, sentTelemetry2.Count);

            MetricTelemetry[] orderedTelemetry = sentTelemetry1
                                                        .OrderByDescending( (t) => ((MetricTelemetry) t).Count * 10000 + ((MetricTelemetry) t).Sum)
                                                        .Select( (t) => (MetricTelemetry) t)
                                                        .ToArray();

            Util.ValidateNumericAggregateValues(orderedTelemetry[0], "Metric A", 8, 916, 118, 111, 2.29128784747792);
            Assert.AreEqual(2, orderedTelemetry[0].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Val", orderedTelemetry[0].Context.Properties["Dim1"]);

            Util.ValidateNumericAggregateValues(orderedTelemetry[1], "Metric A", 8, 836, 108, 101, 2.29128784747792);
            Assert.AreEqual(1, orderedTelemetry[1].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[1].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

            Util.ValidateNumericAggregateValues(orderedTelemetry[2], "Metric A", 4, 1250, 314, 311, 1.11803398874989);
            Assert.AreEqual(2, orderedTelemetry[2].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[2].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Val", orderedTelemetry[2].Context.Properties["Dim1"]);

            Util.ValidateNumericAggregateValues(orderedTelemetry[3], "Metric A", 4, 1210, 304, 301, 1.11803398874989);
            Assert.AreEqual(1, orderedTelemetry[3].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[3].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

            Util.ValidateNumericAggregateValues(orderedTelemetry[4], "Metric A", 2, 631, 316, 315, 0.5);
            Assert.AreEqual(2, orderedTelemetry[4].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[4].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Val", orderedTelemetry[4].Context.Properties["Dim1"]);

            Util.ValidateNumericAggregateValues(orderedTelemetry[5], "Metric A", 2, 611, 306, 305, 0.5);
            Assert.AreEqual(1, orderedTelemetry[5].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[5].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

            orderedTelemetry = sentTelemetry2
                                        .OrderByDescending((t) => ((MetricTelemetry) t).Count * 10000 + ((MetricTelemetry) t).Sum)
                                        .Select((t) => (MetricTelemetry) t)
                                        .ToArray();

            Util.ValidateNumericAggregateValues(orderedTelemetry[0], "Metric A", 2, 423, 212, 211, 0.5);
            Assert.AreEqual(2, orderedTelemetry[0].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Val", orderedTelemetry[0].Context.Properties["Dim1"]);

            Util.ValidateNumericAggregateValues(orderedTelemetry[1], "Metric A", 2, 403, 202, 201, 0.5);
            Assert.AreEqual(1, orderedTelemetry[1].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[1].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));


            Metric metricB21c1 = client21.GetMetric("Metric B", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient);

            TelemetryClient client22 = new TelemetryClient(telemetryPipeline2);
            TelemetryClient client23 = new TelemetryClient(telemetryPipeline2);
            Assert.AreNotSame(metricB21c1, client22.GetMetric("Metric B", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient));
            Assert.AreSame(metricB21c1, client21.GetMetric("Metric B", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient));
            Assert.ThrowsException<ArgumentException>( () => client21.GetMetric("Metric B", MetricConfigurations.Common.Accumulator(), MetricAggregationScope.TelemetryClient) );
            Assert.IsNotNull(client23.GetMetric("Metric B", MetricConfigurations.Common.Accumulator(), MetricAggregationScope.TelemetryClient));

            Metric metricB211 = client21.GetMetric("Metric B", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryConfiguration);

            TelemetryClient client24 = new TelemetryClient(telemetryPipeline2);
            TelemetryClient client25 = new TelemetryClient(telemetryPipeline2);
            Assert.AreSame(metricB211, client24.GetMetric("Metric B", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryConfiguration));
            Assert.AreSame(metricB211, client21.GetMetric("Metric B", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryConfiguration));
            Assert.ThrowsException<ArgumentException>( () => client21.GetMetric("Metric B", MetricConfigurations.Common.Accumulator(), MetricAggregationScope.TelemetryConfiguration) );
            Assert.ThrowsException<ArgumentException>( () => client25.GetMetric("Metric B", MetricConfigurations.Common.Accumulator(), MetricAggregationScope.TelemetryConfiguration) );

            Assert.ThrowsException<ArgumentException>( () => client11.GetMetric("Metric C", MetricConfigurations.Common.Accumulator(), (MetricAggregationScope) 42) );

            Util.CompleteDefaultAggregationCycle(
                        client11.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        client12.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        client21.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        client22.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        client23.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        client24.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        client25.GetMetricManager(MetricAggregationScope.TelemetryClient),
                        telemetryPipeline2.GetMetricManager(),
                        telemetryPipeline1.GetMetricManager());

            telemetryPipeline1.Dispose();
            telemetryPipeline2.Dispose();
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetMetric_RespectsClientContext()
        {
            IList<ITelemetry> sentTelemetry;
            TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out sentTelemetry);
            telemetryPipeline.InstrumentationKey = "754DD89F-61D6-4539-90C7-D886449E12BC";
            TelemetryClient client = new TelemetryClient(telemetryPipeline);

            Metric animalsSold = client.GetMetric("AnimalsSold", "Species", MetricConfigurations.Common.Measurement(), MetricAggregationScope.TelemetryClient);
            animalsSold.TryTrackValue(10, "Cow");
            animalsSold.TryTrackValue(20, "Cow");
            client.GetMetricManager(MetricAggregationScope.TelemetryClient).Flush();

            animalsSold.TryTrackValue(100, "Rabbit");
            animalsSold.TryTrackValue(200, "Rabbit");

            client.Context.InstrumentationKey = "3A3C34B6-CA2D-4372-B772-3B015E1E83DC";
            client.Context.Device.Model = "Super-Fancy";
            client.Context.Properties["MyTag"] = "MyValue";

            animalsSold.TryTrackValue(30, "Cow");
            animalsSold.TryTrackValue(40, "Cow");
            animalsSold.TryTrackValue(300, "Rabbit");
            animalsSold.TryTrackValue(400, "Rabbit");
            client.GetMetricManager(MetricAggregationScope.TelemetryClient).Flush();

            Assert.AreEqual(3, sentTelemetry.Count);

            MetricTelemetry[] orderedTelemetry = sentTelemetry
                                                        .OrderByDescending((t) => ((MetricTelemetry) t).Count * 10000 + ((MetricTelemetry) t).Sum)
                                                        .Select((t) => (MetricTelemetry) t)
                                                        .ToArray();

            Util.ValidateNumericAggregateValues(orderedTelemetry[0], "AnimalsSold", 4, 1000, 400, 100, 111.803398874989);
            Assert.AreEqual(3, orderedTelemetry[0].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[0].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Rabbit", orderedTelemetry[0].Context.Properties["Species"]);
            Assert.AreEqual("MyValue", orderedTelemetry[0].Context.Properties["MyTag"]);
            Assert.AreEqual("Super-Fancy", orderedTelemetry[0].Context.Device.Model);
            Assert.AreEqual("3A3C34B6-CA2D-4372-B772-3B015E1E83DC", orderedTelemetry[0].Context.InstrumentationKey);

            Util.ValidateNumericAggregateValues(orderedTelemetry[1], "AnimalsSold", 2, 70, 40, 30, 5);
            Assert.AreEqual(3, orderedTelemetry[1].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[1].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Cow", orderedTelemetry[1].Context.Properties["Species"]);
            Assert.AreEqual("MyValue", orderedTelemetry[1].Context.Properties["MyTag"]);
            Assert.AreEqual("Super-Fancy", orderedTelemetry[1].Context.Device.Model);
            Assert.AreEqual("3A3C34B6-CA2D-4372-B772-3B015E1E83DC", orderedTelemetry[1].Context.InstrumentationKey);

            Util.ValidateNumericAggregateValues(orderedTelemetry[2], "AnimalsSold", 2, 30, 20, 10, 5);
            Assert.AreEqual(2, orderedTelemetry[2].Context.Properties.Count);
            Assert.IsTrue(orderedTelemetry[2].Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
            Assert.AreEqual("Cow", orderedTelemetry[2].Context.Properties["Species"]);
            Assert.IsNull(orderedTelemetry[2].Context.Device.Model);
            Assert.AreEqual("754DD89F-61D6-4539-90C7-D886449E12BC", orderedTelemetry[2].Context.InstrumentationKey);

            Util.CompleteDefaultAggregationCycle(client.GetMetricManager(MetricAggregationScope.TelemetryClient));
            telemetryPipeline.Dispose();
        }
    }
}
