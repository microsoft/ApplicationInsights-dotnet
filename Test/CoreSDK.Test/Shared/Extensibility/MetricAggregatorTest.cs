
namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class MetricAggregatorTest
    {
        [TestMethod]
        public void MetricAggregatorInvokesMetricProcessorsForEachSample()
        {
            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            var dimensions = new Dictionary<string, string> {
                { "Dim1", "Value1"},
                { "Dim2", "Value2"}
            };

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.GetMetricAggregator("Test Metric", dimensions);
                aggregator.Track(42);
            }

            var sample = (MetricSample)sentSamples.Single();

            Assert.Equal("Test Metric", sample.MetricName);

            Assert.Equal(42, sample.Value);

            Assert.Equal("Value1", sample.Dimensions["Dim1"]);
            Assert.Equal("Value2", sample.Dimensions["Dim2"]);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesSampleCountCorrectly()
        {
            double[] testValues = { 4.45, 8, 29.21, 78.43, 0 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.GetMetricAggregator("Test Metric");

                for (int i = 0; i < testValues.Length; i++)
                {
                    aggregator.Track(testValues[i]);
                }
            }

            int sentSampleCount = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return metric == null ? 0 : metric.Count;
                });

            Assert.Equal(testValues.Length, sentSampleCount);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesSumCorrectly()
        {
            double[] testValues = { 4.45, 8, 29.21, 78.43, 0 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.GetMetricAggregator("Test Metric");

                for (int i = 0; i < testValues.Length; i++)
                {
                    aggregator.Track(testValues[i]);
                }
            }

            double sentSampleSum = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return metric == null ? 0 : metric.Sum;
                });

            Assert.Equal(testValues.Sum(), sentSampleSum);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesMinCorrectly()
        {
            double[] testValues = { 4.45, 8, 29.21, 78.43, 1.4 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.GetMetricAggregator("Test Metric");

                for (int i = 0; i < testValues.Length; i++)
                {
                    aggregator.Track(testValues[i]);
                }
            }

            double sentSampleSum = sentTelemetry.Min(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return metric == null ? 0 : metric.Min;
                });

            Assert.Equal(testValues.Min(), sentSampleSum);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesMaxCorrectly()
        {
            double[] testValues = { 4.45, 8, 29.21, 78.43, 1.4 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.GetMetricAggregator("Test Metric");

                for (int i = 0; i < testValues.Length; i++)
                {
                    aggregator.Track(testValues[i]);
                }
            }

            double sentSampleMax = sentTelemetry.Max(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return metric == null ? 0 : metric.Max;
                });

            Assert.Equal(testValues.Max(), sentSampleMax);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesStandardDeviationCorrectly()
        {
            double[] testValues = { 1, 2, 3, 4, 5 }; 

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.GetMetricAggregator("Test Metric");

                for (int i = 0; i < testValues.Length; i++)
                {
                    aggregator.Track(testValues[i]);
                }
            }

            double sumOfSquares = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return
                    metric == null
                        ? 0
                        : Math.Pow(metric.StandardDeviation, 2) * metric.Count + Math.Pow(metric.Sum, 2) / metric.Count;
                });

            int count = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return metric == null ? 0 : metric.Count;
                });

            double sum = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as AggregatedMetricTelemetry;
                    return metric == null ? 0 : metric.Sum;
                });

            double stddev = Math.Sqrt(sumOfSquares / count - Math.Pow(sum / count, 2));

            Assert.Equal(testValues.StdDev(), stddev);
        }

        private TelemetryClient InitializeTelemetryClient(List<ITelemetry> sentTelemetry, List<MetricSample> sentSamples)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };

            var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = Guid.NewGuid().ToString(), TelemetryChannel = channel };
            telemetryConfiguration.MetricProcessors.Add(new StubMetricProcessor(sentSamples));

            var client = new TelemetryClient(telemetryConfiguration);

            return client;
        }
    }
}
 