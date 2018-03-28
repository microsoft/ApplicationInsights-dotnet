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
    public class MetricManagerV1Test
    {
        [TestMethod]
        public void CanCreateMetricHavingNoDimensions()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                // Act
                MetricV1 metric = manager.CreateMetric("Test Metric");
                metric.Track(42);
            }

            // Assert (single metric aggregation exists in the output)
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.AreEqual("Test Metric", aggregatedMetric.Name);

            Assert.AreEqual(1, aggregatedMetric.Count);
            Assert.AreEqual(42, aggregatedMetric.Sum);

            // note: interval duration property is auto-generated
            Assert.AreEqual(1, aggregatedMetric.Properties.Count);
        }

        [TestMethod]
        public void CanCreateMetricExplicitlySettingDimensionsToNull()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                // Act
                MetricV1 metric = manager.CreateMetric("Test Metric", null);
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.AreEqual("Test Metric", aggregatedMetric.Name);

            Assert.AreEqual(1, aggregatedMetric.Count);
            Assert.AreEqual(42, aggregatedMetric.Sum);

            // note: interval duration property is auto-generated
            Assert.AreEqual(1, aggregatedMetric.Properties.Count);
        }

        [TestMethod]
        public void CanCreateMetricWithASetOfDimensions()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            var dimensions = new Dictionary<string, string> {
                { "Dim1", "Value1"},
                { "Dim2", "Value2"}
            };

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                // Act
                MetricV1 metric = manager.CreateMetric("Test Metric", dimensions);
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.AreEqual("Test Metric", aggregatedMetric.Name);

            Assert.AreEqual(1, aggregatedMetric.Count);
            Assert.AreEqual(42, aggregatedMetric.Sum);

            // note: interval duration property is auto-generated
            Assert.AreEqual(3, aggregatedMetric.Properties.Count);

            Assert.AreEqual("Value1", aggregatedMetric.Properties["Dim1"]);
            Assert.AreEqual("Value2", aggregatedMetric.Properties["Dim2"]);
        }

        [TestMethod]
        public void AggregatedMetricTelemetryHasIntervalDurationProperty()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.AreEqual("Test Metric", aggregatedMetric.Name);

            Assert.AreEqual(1, aggregatedMetric.Count);
            Assert.AreEqual(1, aggregatedMetric.Properties.Count);

            Assert.IsTrue(aggregatedMetric.Properties.ContainsKey("_MS.AggregationIntervalMs"));
        }

        [TestMethod]
        public void AggregatedMetricTelemetryIntervalDurationPropertyIsPositiveInteger()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                // Act
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.AreEqual("Test Metric", aggregatedMetric.Name);

            Assert.AreEqual(1, aggregatedMetric.Count);
            Assert.AreEqual(1, aggregatedMetric.Properties.Count);

            Assert.IsTrue(aggregatedMetric.Properties.ContainsKey("_MS.AggregationIntervalMs"));
            Assert.IsTrue(long.Parse(aggregatedMetric.Properties["_MS.AggregationIntervalMs"]) > 0);
        }

        [TestMethod]
        public void EqualMetricsAreCombinedIntoSignleAggregatedStatsStructure()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            MetricV1 metric1 = null;
            MetricV1 metric2 = null;

            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                // note: on first go aggregators may be different because manager may
                // snapshot after first got created but before the second
                for (int i = 0; i < 2; i++)
                {
                    metric1 = manager.CreateMetric("Test Metric");
                    metric2 = manager.CreateMetric("Test Metric");

                    // Act
                    metric1.Track(10);
                    metric2.Track(5);

                    manager.Flush();

                    if (sentTelemetry.Count == 1)
                    {
                        break;
                    }
                    else
                    {
                        sentTelemetry.Clear();
                    }
                }
            }

            // Assert
            Assert.AreEqual(1, sentTelemetry.Count);

            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.AreEqual(2, aggregatedMetric.Count);
            Assert.AreEqual(15, aggregatedMetric.Sum);
        }

        [TestMethod]
        public void CanDisposeMetricManagerMultipleTimes()
        {
            MetricManagerV1 manager = null;

            using (manager = new MetricManagerV1()) { }

            //Assert.DoesNotThrow
            manager.Dispose();
        }

        [TestMethod]
        public void FlushCreatesAggregatedMetricTelemetry()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                metric.Track(42);

                // Act
                manager.Flush();

                // Assert
                Assert.AreEqual(1, sentTelemetry.Count);

                var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();
                Assert.IsNotNull(aggregatedMetric);
            }
        }

        [TestMethod]
        public void DisposingManagerCreatesAggregatedMetricTelemetry()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManagerV1 manager = new MetricManagerV1(client))
            {
                MetricV1 metric = manager.CreateMetric("Test Metric");

                metric.Track(42);

                // Act
                manager.Dispose();

                // Assert
                Assert.AreEqual(1, sentTelemetry.Count);

                var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();
                Assert.IsNotNull(aggregatedMetric);
            }
        }

        private TelemetryClient InitializeTelemetryClient(List<ITelemetry> sentTelemetry)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var telemetryConfiguration = new TelemetryConfiguration(Guid.NewGuid().ToString(), channel);

            var client = new TelemetryClient(telemetryConfiguration);

            return client;
        }
    }
}
