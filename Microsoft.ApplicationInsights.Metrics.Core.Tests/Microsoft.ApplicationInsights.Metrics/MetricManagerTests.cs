using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.TestUtil;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricManagerTests
    {
        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>( () => new MetricManager(telemetryPipeline: null));

            var manager = new MetricManager(new MemoryMetricTelemetryPipeline());
            Assert.IsNotNull(manager);

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void CreateNewSeries()
        {
            var manager = new MetricManager(new MemoryMetricTelemetryPipeline());

            IMetricSeriesConfiguration config = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);

            Assert.ThrowsException<ArgumentNullException>( () => manager.CreateNewSeries(null, config) );
            Assert.ThrowsException<ArgumentNullException>( () => manager.CreateNewSeries("Foo Bar", null) );

            MetricSeries series = manager.CreateNewSeries("Foo Bar", config);
            Assert.IsNotNull(series);

            Assert.AreEqual(config, series.GetConfiguration());
            Assert.AreSame(config, series.GetConfiguration());

            Assert.AreEqual("Foo Bar", series.MetricId);

            Util.CompleteDefaultAggregationCycle(manager);
        }

        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void Flush()
        {
            {
                var metricsCollector = new MemoryMetricTelemetryPipeline();
                var manager = new MetricManager(metricsCollector);
                manager.Flush();

                Assert.AreEqual(0, metricsCollector.Count);
                Util.CompleteDefaultAggregationCycle(manager);
            }
            {
                var metricsCollector = new MemoryMetricTelemetryPipeline();
                var manager = new MetricManager(metricsCollector);

                IMetricSeriesConfiguration measurementConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false);
                IMetricSeriesConfiguration counterConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: false);

                MetricSeries series1 = manager.CreateNewSeries("Measurement 1", measurementConfig);
                MetricSeries series2 = manager.CreateNewSeries("Measurement 2", measurementConfig);
                MetricSeries series3 = manager.CreateNewSeries("Counter 1", counterConfig);

                series1.TrackValue(1);
                series1.TrackValue(1);
                series1.TrackValue(1);

                series2.TrackValue(-1);
                series2.TrackValue(-1);
                series2.TrackValue(-1);

                series3.TrackValue(-2);
                series3.TrackValue(1);
                series3.TrackValue(1);

                manager.Flush();

                Assert.AreEqual(3, metricsCollector.Count);

                Assert.AreEqual(1, metricsCollector.Where( (item) => (item as MetricTelemetry).Name.Equals("Measurement 1") ).Count());
                Assert.AreEqual(3, (metricsCollector.First( (item) => (item as MetricTelemetry).Name.Equals("Measurement 1") ) as MetricTelemetry).Count);
                Assert.AreEqual(3, (metricsCollector.First( (item) => (item as MetricTelemetry).Name.Equals("Measurement 1") ) as MetricTelemetry).Sum);

                Assert.AreEqual(1, metricsCollector.Where( (item) => (item as MetricTelemetry).Name.Equals("Measurement 2") ).Count());
                Assert.AreEqual(3, (metricsCollector.First( (item) => (item as MetricTelemetry).Name.Equals("Measurement 2") ) as MetricTelemetry).Count);
                Assert.AreEqual(-3, (metricsCollector.First( (item) => (item as MetricTelemetry).Name.Equals("Measurement 2") ) as MetricTelemetry).Sum);

                Assert.AreEqual(1, metricsCollector.Where( (item) => (item as MetricTelemetry).Name.Equals("Counter 1") ).Count());
                Assert.AreEqual(3, (metricsCollector.First( (item) => (item as MetricTelemetry).Name.Equals("Counter 1") ) as MetricTelemetry).Count);
                Assert.AreEqual(0, (metricsCollector.First( (item) => (item as MetricTelemetry).Name.Equals("Counter 1") ) as MetricTelemetry).Sum);

                metricsCollector.Clear();
                Assert.AreEqual(0, metricsCollector.Count);

                manager.Flush();

                Assert.AreEqual(1, metricsCollector.Count);

                Assert.AreEqual(1, metricsCollector.Where((item) => (item as MetricTelemetry).Name.Equals("Counter 1")).Count());
                Assert.AreEqual(3, (metricsCollector.First((item) => (item as MetricTelemetry).Name.Equals("Counter 1")) as MetricTelemetry).Count);
                Assert.AreEqual(0, (metricsCollector.First((item) => (item as MetricTelemetry).Name.Equals("Counter 1")) as MetricTelemetry).Sum);

                Util.CompleteDefaultAggregationCycle(manager);
            }

        }
    }
}
