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
