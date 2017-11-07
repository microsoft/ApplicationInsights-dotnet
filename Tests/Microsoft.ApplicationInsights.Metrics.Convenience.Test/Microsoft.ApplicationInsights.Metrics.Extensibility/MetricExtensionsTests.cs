using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricExtensionsTests
    {
        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void GetConfiguration()
        {
            TelemetryConfiguration pipeline = Util.CreateAITelemetryConfig();
            TelemetryClient client = new TelemetryClient(pipeline);

            {
                Metric metric = client.GetMetric("CowsSold");
                Assert.AreEqual(MetricConfigurations.Measurement, metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Measurement, metric.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("PigsSold", MetricConfigurations.Accumulator);
                Assert.AreEqual(MetricConfigurations.Accumulator, metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Accumulator, metric.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("HorsesSold", MetricConfigurations.Measurement);
                Assert.AreEqual(MetricConfigurations.Measurement, metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Measurement, metric.GetConfiguration());
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(10, 5, new SimpleMetricSeriesConfiguration(false, false));
                Metric metric = client.GetMetric("ChickensSold", config);
                Assert.AreEqual(config, metric.GetConfiguration());
                Assert.AreSame(config, metric.GetConfiguration());
            }

            Util.CompleteDefaultAggregationCycle(pipeline.Metrics());
            pipeline.Dispose();
        }

        ///// <summary />
        //[TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        //[TestMethod]
        //public void GetMetricManager()
        //{
        //    {
        //        TelemetryClient client = new TelemetryClient();
        //        Metric metric = client.GetMetric("CowsSold");
        //        Assert.AreSame(TelemetryConfiguration.Active.Metrics(), metric.GetMetricManager());
        //        Util.CompleteDefaultAggregationCycle(TelemetryConfiguration.Active.Metrics());
        //    }
        //    {
        //        TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig();
        //        TelemetryClient client = new TelemetryClient(telemetryPipeline);
        //        Metric metric = client.GetMetric("CowsSold");
        //        Assert.AreSame(telemetryPipeline.Metrics(), metric.GetMetricManager());
        //        Util.CompleteDefaultAggregationCycle(telemetryPipeline.Metrics());
        //    }
        //}

    }
}
