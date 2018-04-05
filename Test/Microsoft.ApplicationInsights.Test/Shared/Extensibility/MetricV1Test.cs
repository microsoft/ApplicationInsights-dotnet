namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

    [TestClass]
    public class MetricV1Test
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

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric", dimensions);

                // Act
                metric.Track(42);
            }

            // Assert
            var sample = (MetricSample)sentSamples.Single();

            Assert.AreEqual("Test Metric", sample.Name);

            Assert.AreEqual(42, sample.Value);

            Assert.AreEqual("Value1", sample.Dimensions["Dim1"]);
            Assert.AreEqual("Value2", sample.Dimensions["Dim2"]);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesSampleCountCorrectly()
        {
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 0 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
            int sentSampleCount = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return (metric == null) || (!metric.Count.HasValue) ? 0 : metric.Count.Value;
                });

            Assert.AreEqual(testValues.Length, sentSampleCount);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesSumCorrectly()
        {
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 0 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
            double sentSampleSum = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return metric == null ? 0 : metric.Sum;
                });

            Assert.AreEqual(testValues.Sum(), sentSampleSum);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesMinCorrectly()
        {
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 1.4 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
            double sentSampleSum = sentTelemetry.Min(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return (metric == null) || (!metric.Min.HasValue) ? 0 : metric.Min.Value;
                });

            Assert.AreEqual(testValues.Min(), sentSampleSum);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesMaxCorrectly()
        {
            // Arrange
            double[] testValues = { 4.45, 8, 29.21, 78.43, 1.4 };

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
            double sentSampleMax = sentTelemetry.Max(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return (metric == null) || (!metric.Max.HasValue) ? 0 : metric.Max.Value;
                });

            Assert.AreEqual(testValues.Max(), sentSampleMax);
        }

        [TestMethod]
        public void MetricAggregatorCalculatesStandardDeviationCorrectly()
        {
            // Arrange
            double[] testValues = { 1, 2, 3, 4, 5 }; 

            var sentTelemetry = new List<ITelemetry>();
            var sentSamples = new List<MetricSample>();

            var client = this.InitializeTelemetryClient(sentTelemetry, sentSamples);

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                for (int i = 0; i < testValues.Length; i++)
                {
                    metric.Track(testValues[i]);
                }
            }

            // Assert
            double sumOfSquares = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return
                    metric == null
                        ? 0
                        : Math.Pow(metric.StandardDeviation.Value, 2) * metric.Count.Value + Math.Pow(metric.Sum, 2) / metric.Count.Value;
                });

            int count = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return metric == null ? 0 : metric.Count.Value;
                });

            double sum = sentTelemetry.Sum(
                (telemetry) => {
                    var metric = telemetry as MetricTelemetry;
                    return metric == null ? 0 : metric.Sum;
                });

            double stddev = Math.Sqrt(sumOfSquares / count - Math.Pow(sum / count, 2));

            Assert.AreEqual(testValues.StdDev(), stddev);
        }

        #region Equitable<T> implementation tests

        [TestMethod]
        public void MetricNeverEqualsNull()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric");
                object other = null;

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricEqualsItself()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric");

                Assert.IsTrue(metric.Equals(metric));
            }
        }

        [TestMethod]
        public void MetricNotEqualsOtherObject()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric");
                var other = new object();

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricsAreEqualForTheSameMetricNameWithoutDimensions()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric");
                MetricV1 other = manager.CreateMetric("My metric");

                Assert.IsTrue(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricNameIsCaseSensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric");
                MetricV1 other = manager.CreateMetric("My Metric");

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricNameIsAccentSensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric");
                MetricV1 other = manager.CreateMetric("My métric");

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricsAreEqualIfDimensionsSetToNothingImplicitlyAndExplicitly()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric", null);
                MetricV1 other = manager.CreateMetric("My metric");

                Assert.IsTrue(metric.Equals(other));
            }
        }

        [TestMethod]
        public void MetricsAreEqualIfDimensionsSetToNothingImplicitlyAndExplicitlyAsEmptySet()
        {
            using (var manager = new MetricManagerV1())
            {
                MetricV1 metric = manager.CreateMetric("My metric", new Dictionary<string, string>());
                MetricV1 other = manager.CreateMetric("My metric");

                Assert.IsTrue(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionsAreOrderInsensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                var dimensionSet1 = new Dictionary<string, string>() {
                    { "Dim1", "Value1"},
                    { "Dim2", "Value2"},
                };

                var dimensionSet2 = new Dictionary<string, string>() {
                    { "Dim2", "Value2"},
                    { "Dim1", "Value1"},
                };

                MetricV1 metric = manager.CreateMetric("My metric", dimensionSet1);
                MetricV1 other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.IsTrue(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionNamesAreCaseSensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "dim1", "Value1" } };

                MetricV1 metric = manager.CreateMetric("My metric", dimensionSet1);
                MetricV1 other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionNamesAreAccentSensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "Dím1", "Value1" } };

                MetricV1 metric = manager.CreateMetric("My metric", dimensionSet1);
                MetricV1 other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionValuesAreCaseSensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "Dim1", "value1" } };

                MetricV1 metric = manager.CreateMetric("My metric", dimensionSet1);
                MetricV1 other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.IsFalse(metric.Equals(other));
            }
        }

        [TestMethod]
        public void DimensionValuesAreAccentSensitive()
        {
            using (var manager = new MetricManagerV1())
            {
                var dimensionSet1 = new Dictionary<string, string>() { { "Dim1", "Value1" } };
                var dimensionSet2 = new Dictionary<string, string>() { { "Dim1", "Válue1" } };

                MetricV1 metric = manager.CreateMetric("My metric", dimensionSet1);
                MetricV1 other = manager.CreateMetric("My metric", dimensionSet2);

                Assert.IsFalse(metric.Equals(other));
            }
        }

        #endregion

        private TelemetryClient InitializeTelemetryClient(List<ITelemetry> sentTelemetry, List<MetricSample> sentSamples)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };

            var telemetryConfiguration = new TelemetryConfiguration(Guid.NewGuid().ToString(), channel);
            telemetryConfiguration.MetricProcessors.Add(new StubMetricProcessorV1(sentSamples));

            var client = new TelemetryClient(telemetryConfiguration);

            return client;
        }
    }
}
 