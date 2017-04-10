namespace Unit.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Filtering;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseMetricProcessorTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseTestHelper.ClearEnvironment();
        }

        [TestMethod]
        public void QuickPulseMetricProcessorCollectsCalculatedMetrics()
        {
            // ARRANGE
            var metrics = new[]
            {
                new CalculatedMetricInfo()
                {
                    Id = "Metric1",
                    TelemetryType = TelemetryType.Metric,
                    Projection = "Awesome1",
                    Aggregation = AggregationType.Avg,
                    FilterGroups = new FilterConjunctionGroupInfo[0]
                },
                new CalculatedMetricInfo()
                {
                    Id = "Metric2",
                    TelemetryType = TelemetryType.Metric,
                    Projection = "Awesome2",
                    Aggregation = AggregationType.Sum,
                    FilterGroups = new FilterConjunctionGroupInfo[0]
                }
            };

            CollectionConfigurationError[] errors;
            var collectionConfiguration = new CollectionConfiguration(
                new CollectionConfigurationInfo() { Metrics = metrics },
                out errors,
                new ClockMock());
            var accumulatorManager = new QuickPulseDataAccumulatorManager(collectionConfiguration);
            var metricProcessor = new QuickPulseMetricProcessor();
            var metric1 = new MetricManager().CreateMetric("Awesome1");
            var metric2 = new MetricManager().CreateMetric("Awesome2");

            metricProcessor.StartCollection(accumulatorManager);

            // ACT
            metricProcessor.Track(metric1, 1.0d);
            metricProcessor.Track(metric1, 2.0d);
            metricProcessor.Track(metric1, 3.0d);
            metricProcessor.Track(metric2, 10.0d);
            metricProcessor.Track(metric2, 20.0d);
            metricProcessor.Track(metric2, 30.0d);

            metricProcessor.StopCollection();

            // ASSERT
            Dictionary<string, AccumulatedValues> calculatedMetrics =
                accumulatorManager.CurrentDataAccumulator.CollectionConfigurationAccumulator.MetricAccumulators;

            Assert.AreEqual(2, calculatedMetrics.Count);

            Assert.AreEqual(2d, calculatedMetrics["Metric1"].CalculateAggregation(out long count));
            Assert.AreEqual(3, count);
            Assert.AreEqual(10d + 20d + 30d, calculatedMetrics["Metric2"].CalculateAggregation(out count));
            Assert.AreEqual(3, count);
        }
    }
}