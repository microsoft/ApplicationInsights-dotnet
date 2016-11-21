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
    public class MetricManagerTest
    {
        [TestMethod]
        public void CanCreateMetricHavingNoDimensions()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManager manager = new MetricManager(client))
            {
                // Act
                Metric metric = manager.CreateMetric("Test Metric");
                metric.Track(42);
            }

            // Assert (single metric aggregation exists in the output)
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);

            // note: interval duration property is auto-generated
            Assert.Equal(1, aggregatedMetric.Properties.Count);
        }

        [TestMethod]
        public void CanCreateMetricExplicitlySettingDimensionsToNull()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            using (MetricManager manager = new MetricManager(client))
            {
                // Act
                Metric metric = manager.CreateMetric("Test Metric", null);
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);

            // note: interval duration property is auto-generated
            Assert.Equal(1, aggregatedMetric.Properties.Count);
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

            using (MetricManager manager = new MetricManager(client))
            {
                // Act
                Metric metric = manager.CreateMetric("Test Metric", dimensions);
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);

            // note: interval duration property is auto-generated
            Assert.Equal(3, aggregatedMetric.Properties.Count);

            Assert.Equal("Value1", aggregatedMetric.Properties["Dim1"]);
            Assert.Equal("Value2", aggregatedMetric.Properties["Dim2"]);
        }

        [TestMethod]
        public void AggregatedMetricTelemetryHasIntervalDurationProperty()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(1, aggregatedMetric.Properties.Count);

            Assert.True(aggregatedMetric.Properties.ContainsKey("IntervalDurationMs"));
        }

        [TestMethod]
        public void AggregatedMetricTelemetryIntervalDurationPropertyIsPositiveInteger()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                // Act
                metric.Track(42);
            }

            // Assert
            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(1, aggregatedMetric.Properties.Count);

            Assert.True(aggregatedMetric.Properties.ContainsKey("IntervalDurationMs"));
            Assert.True(long.Parse(aggregatedMetric.Properties["IntervalDurationMs"]) > 0);
        }

        [TestMethod]
        public void EqualMetricsAreCombinedIntoSignleAggregatedStatsStructure()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            Metric metric1 = null;
            Metric metric2 = null;

            using (MetricManager manager = new MetricManager(client))
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
            Assert.Equal(1, sentTelemetry.Count);

            var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal(2, aggregatedMetric.Count);
            Assert.Equal(15, aggregatedMetric.Sum);
        }

        [TestMethod]
        public void CanDisposeMetricManagerMultipleTimes()
        {
            MetricManager manager = null;

            using (manager = new MetricManager()) { }

            Assert.DoesNotThrow(() => { manager.Dispose(); });
        }

        [TestMethod]
        public void FlushCreatesAggregatedMetricTelemetry()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                metric.Track(42);

                // Act
                manager.Flush();

                // Assert
                Assert.Equal(1, sentTelemetry.Count);

                var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();
                Assert.NotNull(aggregatedMetric);
            }
        }

        [TestMethod]
        public void DisposingManagerCreatesAggregatedMetricTelemetry()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricManager manager = new MetricManager(client))
            {
                Metric metric = manager.CreateMetric("Test Metric");

                metric.Track(42);

                // Act
                manager.Dispose();

                // Assert
                Assert.Equal(1, sentTelemetry.Count);

                var aggregatedMetric = (MetricTelemetry)sentTelemetry.Single();
                Assert.NotNull(aggregatedMetric);
            }
        }

        private TelemetryClient InitializeTelemetryClient(List<ITelemetry> sentTelemetry)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = Guid.NewGuid().ToString(), TelemetryChannel = channel };

            var client = new TelemetryClient(telemetryConfiguration);

            return client;
        }
    }
}
