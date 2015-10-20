namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class AdaptiveSamplingTelemetryProcessorTest
    {
        [TestMethod]
        public void AllTelemetryCapturedWhenProductionRateIsLow()
        {
            var sentTelemetry = new List<ITelemetry>();

            var channelBuilder = new TelemetryChannelBuilder();

            int itemsProduced = 0;

            // set up addaptive sampling that evaluates and changes sampling % frequently
            channelBuilder
                .UseAdaptiveSampling(
                    100, 
                    new SamplingPercentageEstimatorSettings()
                    {
                        EvaluationIntervalSeconds = 1,
                        SamplingPercentageDecreaseTimeoutSeconds = 2,
                        SamplingPercentageIncreaseTimeoutSeconds = 2}, 
                    this.TraceSamplingPercentageEvaluation)
                .Use((next) => new StubTelemetryProcessor(next) { OnProcess = (t) => sentTelemetry.Add(t) });

            ITelemetryChannel channel = channelBuilder.Build();

            const int productionFrequencyMs = 1000;

            using (var productionTimer = new Timer(
                        (state) => { channel.Send(new RequestTelemetry()); itemsProduced++; },
                        null,
                        productionFrequencyMs,
                        productionFrequencyMs))
            {
                Thread.Sleep(25000);
            }

            Assert.Equal(itemsProduced, sentTelemetry.Count);
        }

        [TestMethod]
        public void SamplingPercentageAdjustsAccordingToConstantHighProductionRate()
        {
            var sentTelemetry = new List<ITelemetry>();

            var channelBuilder = new TelemetryChannelBuilder();

            int itemsProduced = 0;

            // set up addaptive sampling that evaluates and changes sampling % frequently
            channelBuilder
                .UseAdaptiveSampling(
                    100,
                    new SamplingPercentageEstimatorSettings()
                    {
                        EvaluationIntervalSeconds = 2,
                        SamplingPercentageDecreaseTimeoutSeconds = 2,
                        SamplingPercentageIncreaseTimeoutSeconds = 2
                    },
                    this.TraceSamplingPercentageEvaluation)
                .Use((next) => new StubTelemetryProcessor(next) { OnProcess = (t) => sentTelemetry.Add(t) });

            ITelemetryChannel channel = channelBuilder.Build();

            const int productionFrequencyMs = 100;

            using (var productionTimer = new Timer(
                        (state) => 
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                channel.Send(new RequestTelemetry());
                                itemsProduced++;
                            }
                        },
                        null,
                        0,
                        productionFrequencyMs))
            {
                Thread.Sleep(25000);
            }

            // number of items produced should be close to target of 5/second
            int targetItemCount = 25 * 5;

            Trace.WriteLine(string.Format("'Ideal' telemetry item count: {0}", targetItemCount));
            Trace.WriteLine(string.Format(
                "Expected range (+-30%): from {0} to {1}",
                targetItemCount - targetItemCount * 1 / 3,
                targetItemCount + targetItemCount * 1 / 3));
            Trace.WriteLine(string.Format(
                "Actual telemetry item count: {0} ({1:##.##}% of ideal)", 
                sentTelemetry.Count,
                100.0 * sentTelemetry.Count / targetItemCount));

            Assert.True(sentTelemetry.Count > targetItemCount - targetItemCount * 1/3);
            Assert.True(sentTelemetry.Count < targetItemCount + targetItemCount * 1/3);
        }

        private void TraceSamplingPercentageEvaluation(
            double afterSamplingTelemetryItemRatePerSecond,
            double currentSamplingPercentage,
            double newSamplingPercentage,
            bool isSamplingPercentageChanged,
            SamplingPercentageEstimatorSettings settings)
        {
            Trace.WriteLine(string.Format(
                "[Sampling% evaluation] {0}, Eps: {1}, Current %: {2}, New %: {3}, Changed: {4}",
                DateTimeOffset.UtcNow.ToString("o"), 
                afterSamplingTelemetryItemRatePerSecond,
                currentSamplingPercentage,
                newSamplingPercentage,
                isSamplingPercentageChanged));
        }
    }
}
