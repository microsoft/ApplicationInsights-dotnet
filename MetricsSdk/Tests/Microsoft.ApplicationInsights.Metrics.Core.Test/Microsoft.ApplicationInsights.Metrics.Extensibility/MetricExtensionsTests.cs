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
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("PigsSold", MetricConfigurations.Common.Accumulator());
                Assert.AreEqual(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Accumulator(), metric.GetConfiguration());
            }
            {
                Metric metric = client.GetMetric("HorsesSold", MetricConfigurations.Common.Measurement());
                Assert.AreEqual(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
                Assert.AreSame(MetricConfigurations.Common.Measurement(), metric.GetConfiguration());
            }
            {
                IMetricConfiguration config = new SimpleMetricConfiguration(10, 5, new MetricSeriesConfigurationForMeasurement(false));
                Metric metric = client.GetMetric("ChickensSold", config);
                Assert.AreEqual(config, metric.GetConfiguration());
                Assert.AreSame(config, metric.GetConfiguration());
            }

            Util.CompleteDefaultAggregationCycle(pipeline.GetMetricManager());
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
        //        Assert.AreSame(TelemetryConfiguration.Active.GetMetricManager(), metric.GetMetricManager());
        //        Util.CompleteDefaultAggregationCycle(TelemetryConfiguration.Active.GetMetricManager());
        //    }
        //    {
        //        TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig();
        //        TelemetryClient client = new TelemetryClient(telemetryPipeline);
        //        Metric metric = client.GetMetric("CowsSold");
        //        Assert.AreSame(telemetryPipeline.GetMetricManager(), metric.GetMetricManager());
        //        Util.CompleteDefaultAggregationCycle(telemetryPipeline.GetMetricManager());
        //    }
        //}

    }
}
