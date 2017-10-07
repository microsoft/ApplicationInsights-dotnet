using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;

using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    [TestClass]
    public class ApplicationInsightsTelemetryPipelineTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            {
                Assert.ThrowsException<ArgumentNullException>( () => new ApplicationInsightsTelemetryPipeline(null) );
            }
            {
                TelemetryConfiguration defaultPipeline = TelemetryConfiguration.Active;
                using (defaultPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(defaultPipeline);
                    Assert.IsNotNull(pipelineAdapter);
                }
            }
        }

        /// <summary />
        [TestMethod]
        public async Task TrackAsync()
        {
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                using (var cancelControl = new CancellationTokenSource())
                {
                    cancelControl.Cancel();
                    await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => pipelineAdapter.TrackAsync(null, cancelControl.Token));
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                using (var cancelControl = new CancellationTokenSource())
                {
                    cancelControl.Cancel();
                    await Assert.ThrowsExceptionAsync<ArgumentException>(() => pipelineAdapter.TrackAsync("something", cancelControl.Token));
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                using (var cancelControl = new CancellationTokenSource())
                {
                    cancelControl.Cancel();
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => pipelineAdapter.TrackAsync(new MetricTelemetry(), cancelControl.Token));
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                await pipelineAdapter.TrackAsync(new MetricTelemetry("M1", 10), CancellationToken.None);
                await pipelineAdapter.TrackAsync(new MetricTelemetry("M2", 20), CancellationToken.None);
                await pipelineAdapter.TrackAsync(new MetricTelemetry("M3", 30), CancellationToken.None);
                await pipelineAdapter.TrackAsync(new MetricTelemetry("M4", -40), CancellationToken.None);

                Assert.IsNotNull(telemetrySentToChannel);
                Assert.AreEqual(4, telemetrySentToChannel.Count);

                Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M1") ).Count());
                Assert.AreEqual(1, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M1") ) as MetricTelemetry).Count);
                Assert.AreEqual(10, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M1") ) as MetricTelemetry).Sum);

                Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M2") ).Count());
                Assert.AreEqual(1, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M2") ) as MetricTelemetry).Count);
                Assert.AreEqual(20, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M2") ) as MetricTelemetry).Sum);

                Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M3") ).Count());
                Assert.AreEqual(1, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M3") ) as MetricTelemetry).Count);
                Assert.AreEqual(30, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M3") ) as MetricTelemetry).Sum);

                Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M4") ).Count());
                Assert.AreEqual(1, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M4") ) as MetricTelemetry).Count);
                Assert.AreEqual(-40, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M4") ) as MetricTelemetry).Sum);
            }
        }
    }
}
