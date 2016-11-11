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
    public class MetricAggregatorManagerTest
    {
        [TestMethod]
        public void MetricAggregatorMayBeCreatedForMetricHavingNoDimensions()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                // Act
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric");
                aggregator.Track(42);
            }

            // Assert
            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);
            Assert.Equal(42, aggregatedMetric.Min);
            Assert.Equal(42, aggregatedMetric.Max);
            Assert.Equal(0, aggregatedMetric.StandardDeviation);

            // note: interval duration property is auto-generated
            Assert.Equal(1, aggregatedMetric.Properties.Count);
        }

        [TestMethod]
        public void MetricAggregatorMayBeCreatedExplicitlySettingDimensionsToNull()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                // Act
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric", null);
                aggregator.Track(42);
            }

            // Assert
            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);
            Assert.Equal(42, aggregatedMetric.Min);
            Assert.Equal(42, aggregatedMetric.Max);
            Assert.Equal(0, aggregatedMetric.StandardDeviation);

            // note: interval duration property is auto-generated
            Assert.Equal(1, aggregatedMetric.Properties.Count);
        }

        [TestMethod]
        public void MetricAggregatorMayBeCreatedWithASetOfDimensions()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            var dimensions = new Dictionary<string, string> {
                { "Dim1", "Value1"},
                { "Dim2", "Value2"}
            };

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                // Act
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric", dimensions);
                aggregator.Track(42);
            }

            // Assert
            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);
            Assert.Equal(42, aggregatedMetric.Min);
            Assert.Equal(42, aggregatedMetric.Max);
            Assert.Equal(0, aggregatedMetric.StandardDeviation);

            // note: interval duration property is auto-generated
            Assert.Equal(3, aggregatedMetric.Properties.Count);

            Assert.Equal("Value1", aggregatedMetric.Properties["Dim1"]);
            Assert.Equal("Value2", aggregatedMetric.Properties["Dim2"]);
        }

        [TestMethod]
        public void MetricAggregatorsFlushedWhenManagerIsDisposed()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            // Act
            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric");
                aggregator.Track(42);
            }

            // Assert
            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(42, aggregatedMetric.Sum);
            Assert.Equal(42, aggregatedMetric.Min);
            Assert.Equal(42, aggregatedMetric.Max);
            Assert.Equal(0, aggregatedMetric.StandardDeviation);

            // note: interval duration property is auto-generated
            Assert.Equal(1, aggregatedMetric.Properties.Count);
        }

        [TestMethod]
        public void AggregatedMetricTelemetryHasIntervalDurationProperty()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric");

                // Act
                aggregator.Track(42);
            }

            // Assert
            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

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
            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric");

                // Act
                aggregator.Track(42);
            }

            // Assert
            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", aggregatedMetric.Name);

            Assert.Equal(1, aggregatedMetric.Count);
            Assert.Equal(1, aggregatedMetric.Properties.Count);

            Assert.True(aggregatedMetric.Properties.ContainsKey("IntervalDurationMs"));
            Assert.True(long.Parse(aggregatedMetric.Properties["IntervalDurationMs"]) > 0);
        }

        [TestMethod]
        public void EqualAggregatorsAreCombinedIntoSignleStatsStructure()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);

            MetricAggregator aggregator1 = null;
            MetricAggregator aggregator2 = null;

            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                // note: on first go aggregators may be different because manager may
                // snapshot after first got created but before the second
                for (int i = 0; i < 2; i++)
                {
                    aggregator1 = manager.CreateMetricAggregator("Test Metric");
                    aggregator2 = manager.CreateMetricAggregator("Test Metric");

                    // Act
                    aggregator1.Track(10);
                    aggregator2.Track(5);

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

            var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();

            Assert.Equal(2, aggregatedMetric.Count);
            Assert.Equal(15, aggregatedMetric.Sum);
        }

        [TestMethod]
        public void CantCreateAggregatorUsingDisposedAggregatorManager()
        {
            MetricAggregatorManager manager = null;

            using (manager = new MetricAggregatorManager()) { }

            Assert.Throws<ObjectDisposedException>(() => { var aggregator = manager.CreateMetricAggregator("My Metric"); });
        }

        [TestMethod]
        public void CantFlushUsingDisposedAggregatorManager()
        {
            MetricAggregatorManager manager = null;

            using (manager = new MetricAggregatorManager()) { }

            Assert.Throws<ObjectDisposedException>(() => { manager.Flush(); });
        }

        [TestMethod]
        public void CantTrackValueonAggregatorCreatedViaDisposedAggregatorManager()
        {
            MetricAggregator aggregator = null;

            using (MetricAggregatorManager manager = new MetricAggregatorManager())
            {
                aggregator = manager.CreateMetricAggregator("My metric");
            }

            Assert.Throws<ObjectDisposedException>(() => { aggregator.Track(42); });
        }

        [TestMethod]
        public void CanDisposeAggregatorManagerMultipleTimes()
        {
            MetricAggregatorManager manager = null;

            using (manager = new MetricAggregatorManager()) { }

            Assert.DoesNotThrow(() => { manager.Dispose(); });
        }

        [TestMethod]
        public void FlushCreatesAggregatedMetricTelemetry()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric");

                aggregator.Track(42);

                // Act
                manager.Flush();

                // Assert
                Assert.Equal(1, sentTelemetry.Count);

                var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();
                Assert.NotNull(aggregatedMetric);
            }
        }

        [TestMethod]
        public void DisposingManagerCreatesAggregatedMetricTelemetry()
        {
            // Arrange
            var sentTelemetry = new List<ITelemetry>();

            var client = this.InitializeTelemetryClient(sentTelemetry);
            using (MetricAggregatorManager manager = new MetricAggregatorManager(client))
            {
                MetricAggregator aggregator = manager.CreateMetricAggregator("Test Metric");

                aggregator.Track(42);

                // Act
                manager.Dispose();

                // Assert
                Assert.Equal(1, sentTelemetry.Count);

                var aggregatedMetric = (AggregatedMetricTelemetry)sentTelemetry.Single();
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
