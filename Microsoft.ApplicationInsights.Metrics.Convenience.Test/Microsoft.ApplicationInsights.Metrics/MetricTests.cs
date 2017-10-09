using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics;
//using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using Microsoft.ApplicationInsights.Channel;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricTests
    {
        /// <summary />
        [TestCategory(TestCategoryNames.NeedsAggregationCycleCompletion)]
        [TestMethod]
        public void Create()
        {
            // The Metric ctor is internal:
            // internal Metric(MetricManager metricManager, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration configuration)
            // It is invoked by
            // private static TelemetryClientExtensions.GetOrCreateMetric(TelemetryClient telemetryClient, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration metricConfiguration)
            // and in turn by
            // public static Metric GetMetric(this TelemetryClient telemetryClient, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration metricConfiguration)
            // We will need to perform this call chan to get the ctor called.

            MemoryMetricTelemetryPipeline telemetryCollector = new MemoryMetricTelemetryPipeline();
            MetricManager metricManager = new MetricManager(telemetryCollector);

            // ** Validate metricManager parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => CreateMetric(
                                    metricManager: null,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "  Foo ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
            }


            // ** Validate metricId parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: null,
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "   ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "  Foo ",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual("Foo", metric.MetricId);
            }


            // ** Validate dimension1Name parameter:

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "  \t",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: " D1  ",
                                    dimension2Name: "D2",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
            }


            // ** Validate dimension2Name parameter:

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "",
                                    configuration: MetricConfiguration.Measurement)
            );

            Assert.ThrowsException<ArgumentException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: " \r\n",
                                    configuration: MetricConfiguration.Measurement)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2\t",
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(2, metric.DimensionsCount);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.AreEqual("D2", metric.GetDimensionName(2));
            }
            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: null,
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(1, metric.DimensionsCount);
                Assert.AreEqual("D1", metric.GetDimensionName(1));
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
            }
            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);
                Assert.AreEqual(0, metric.DimensionsCount);
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(1) );
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => metric.GetDimensionName(2) );
            }


            // ** Validate configuration parameter:

            Assert.ThrowsException<ArgumentNullException>(
                    () => CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: "D1",
                                    dimension2Name: "D2",
                                    configuration: null)
            );

            {
                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: MetricConfiguration.Measurement);
                Assert.IsNotNull(metric);

                Assert.AreEqual(MetricConfiguration.Measurement, metric.GetConfiguration());
                Assert.AreSame(MetricConfiguration.Measurement, metric.GetConfiguration());
            }
            {
                IMetricConfiguration customConfig = new SimpleMetricConfiguration(
                                                                    seriesCountLimit: 10,
                                                                    valuesPerDimensionLimit: 10,
                                                                    seriesConfig: new SimpleMetricSeriesConfiguration(
                                                                                                            lifetimeCounter: true,
                                                                                                            restrictToUInt32Values: true));

                Metric metric = CreateMetric(
                                    metricManager,
                                    metricId: "Foo",
                                    dimension1Name: null,
                                    dimension2Name: null,
                                    configuration: customConfig);
                Assert.IsNotNull(metric);

                Assert.AreEqual(customConfig, metric.GetConfiguration());
                Assert.AreSame(customConfig, metric.GetConfiguration());
            }

            Util.CompleteDefaultAggregationCycle(metricManager);

        }

        private static Metric CreateMetric(MetricManager metricManager, string metricId, string dimension1Name, string dimension2Name, IMetricConfiguration configuration)
        {
            // Metric ctor is private..

            Type apiType = typeof(Metric);
            const string apiName = "TelemetryConfiguration";

            ConstructorInfo ctor = apiType.GetTypeInfo().GetConstructor(
                                                                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                                                                    binder: null,
                                                                    types: new Type[] { typeof(MetricManager),
                                                                                        typeof(string),
                                                                                        typeof(string),
                                                                                        typeof(string),
                                                                                        typeof(IMetricConfiguration)},
                                                                    modifiers: null);

            if (ctor == null)
            {
                throw new InvalidOperationException($"Could not get ConstructorInfo for {apiType.Name}.{apiName} via reflection."
                                                   + " This is either an internal SDK bug or there is a mismatch between the Metrics-SDK version"
                                                   + " and the Application Insights Base SDK version. Please report this issue.");
            }

            try
            {
                object metricObject = ctor.Invoke(new object[] { metricManager, metricId, dimension1Name, dimension2Name, configuration });

                Assert.IsNotNull(metricObject);
                Assert.IsInstanceOfType(metricObject, typeof(Metric));

                return (Metric) metricObject;
            }
            catch(TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                }
                else
                {
                    ExceptionDispatchInfo.Capture(tie).Throw();
                }

                return null; // Never reached.
            }
        }
    }
}
