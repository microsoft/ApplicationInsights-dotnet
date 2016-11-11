
namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class MetricTest
    {
        [TestMethod]
        public void MetricInvokesMetricProcessorsForEachValueTracked()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            var dimensions = new Dictionary<string, string> {
                { "Dim1", "Value1"},
                { "Dim2", "Value2"}
            };

            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric", dimensions);

                // Act
                metric.Track(42);
            }

            // Assert
            var sample = (MetricSample)sentSamples.Single();

            Assert.Equal("Test Metric", sample.MetricName);

            Assert.Equal(42, sample.Value);

            Assert.Equal("Value1", sample.Dimensions["Dim1"]);
            Assert.Equal("Value2", sample.Dimensions["Dim2"]);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesSampleCountCorrectly()
        {
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 0 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
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
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 0 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
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
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 1.4 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
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
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 1.4 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
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
            // Arrange
            double[] testValues = { 1, 2, 3, 4, 5 }; 

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
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

        #region Equitable<T> implementation tests

        [TestMethod]
        public void MetricNeverEqualsNull()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric");
                object other = null;

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricEqualsItself()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric");

                Assert.True(metric.Equals(metric));
            }
        }

        [TestMethod]
        public void MetricNotEqualsOtherObject()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric");
                var other = new object();

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricsAreEqualForTheSameMetricNameWithoutDimensions()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric");
                Metric other = manager.CreateMetric("My metric");

                Assert.True(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricNameIsCaseSensitive()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric");
                Metric other = manager.CreateMetric("My Metric");

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricNameIsAccentSensitive()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric");
                Metric other = manager.CreateMetric("My métric");

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricsAreEqualIfDimensionsSetToNothingImplicitlyAndExplicitly()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric", null);
                Metric other = manager.CreateMetric("My metric");

                Assert.True(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricsAreEqualIfDimensionsSetToNothingImplicitlyAndExplicitlyAsEmptySet()
        {
            using (var manager = new MetricManager())
            {
                Metric metric = manager.CreateMetric("My metric", new Dictionary<string, string>());
                Metric other = manager.CreateMetric("My metric");

                Assert.True(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionsAreOrderInsensitive()
        {
            using (var manager = new MetricManager())
            {
                var dimensionSet1 = new Dictionary<string, string>() {
                    { "Dim1", "Value1"},
                    { "Dim2", "Value2"},
                };

                var dimensionSet2 = new Dictionary<string, string>() {
                    { "Dim2", "Value2"},
                    { "Dim1", "Value1"},
                };

                Metric metric = manager.CreateMetric("My metric", dimensionSet1);
                Metric other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.True(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionNamesAreCaseSensitive()
        {
            using (var manager = new MetricManager())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "dim1", "Value1" } };

                Metric metric = manager.CreateMetric("My metric", dimensionSet1);
                Metric other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionNamesAreAccentSensitive()
        {
            using (var manager = new MetricManager())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "Dím1", "Value1" } };

                Metric metric = manager.CreateMetric("My metric", dimensionSet1);
                Metric other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionValuesAreCaseSensitive()
        {
            using (var manager = new MetricManager())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "Dim1", "value1" } };

                Metric metric = manager.CreateMetric("My metric", dimensionSet1);
                Metric other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.False(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionValuesAreAccentSensitive()
        {
            using (var manager = new MetricManager())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "Dim1", "Válue1" } };

                Metric metric = manager.CreateMetric("My metric", dimensionSet1);
                Metric other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.False(metric.Equals(other));
            }
        }

        #endregion

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
 