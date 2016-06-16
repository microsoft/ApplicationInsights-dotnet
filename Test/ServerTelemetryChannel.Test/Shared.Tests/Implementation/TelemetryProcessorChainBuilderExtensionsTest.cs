namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryProcessorChainBuilderExtensionsTest
    {
        [TestMethod]
        public void UseSamplingThrowsArgumentNullExceptionBuilderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => TelemetryProcessorChainBuilderExtensions.UseSampling(null, 5));
        }

        [TestMethod]
        public void UseSamplingSetsAddsSamplingProcessorToTheChainWithCorrectPercentage()
        {
            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseSampling(5);
            channelBuilder.Build();

            Assert.Equal(5, ((SamplingTelemetryProcessor) tc.TelemetryProcessorChain.FirstTelemetryProcessor).SamplingPercentage);
        }

        [TestMethod]
        public void UseSamplingSetsAddsSamplingProcessorToTheChainWithCorrectExcludedTypes()
        {
            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseSampling(5, "request");
            channelBuilder.Build();

            Assert.Equal(5, ((SamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).SamplingPercentage);
            Assert.Equal("request", ((SamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).ExcludedTypes);
        }

        [TestMethod]
        public void UseAdaptiveSamplingAddsAdaptiveSamplingProcessorToTheChain()
        {
            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseAdaptiveSampling();
            channelBuilder.Build();

            Assert.IsType<AdaptiveSamplingTelemetryProcessor>(tc.TelemetryProcessorChain.FirstTelemetryProcessor);
        }

        [TestMethod]
        public void UseAdaptiveSamplingAddsAdaptiveSamplingProcessorToTheChainWithCorrectExcludedTypes()
        {
            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseAdaptiveSampling("request");
            channelBuilder.Build();

            Assert.Equal("request", ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).ExcludedTypes);
        }

        [TestMethod]
        public void UseAdaptiveSamplingAddsAdaptiveSamplingProcessorToTheChainWithCorrectMaxTelemetryItemsPerSecond()
        {
            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseAdaptiveSampling(5);
            channelBuilder.Build();

            Assert.Equal(5, ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).MaxTelemetryItemsPerSecond);
        }

        [TestMethod]
        public void UseAdaptiveSamplingAddsAdaptiveSamplingProcessorToTheChainWithCorrectMaxTelemetryItemsPerSecondAndExcludedTypes()
        {
            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseAdaptiveSampling(5, "request");
            channelBuilder.Build();

            Assert.Equal(5, ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).MaxTelemetryItemsPerSecond);
            Assert.Equal("request", ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).ExcludedTypes);
        }

        [TestMethod]
        public void UseAdaptiveSamplingAddsAdaptiveSamplingProcessorToTheChainWithCorrectSettings()
        {
            SamplingPercentageEstimatorSettings settings = new SamplingPercentageEstimatorSettings
            {
                MaxSamplingPercentage = 13
            };
            AdaptiveSamplingPercentageEvaluatedCallback callback = (second, percentage, samplingPercentage, changed, estimatorSettings) => { };

            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseAdaptiveSampling(settings, callback);
            channelBuilder.Build();

            Assert.Equal(13, ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).MaxSamplingPercentage);
        }

        [TestMethod]
        public void UseAdaptiveSamplingAddsAdaptiveSamplingProcessorToTheChainWithCorrectSettingsAndExcludedTypes()
        {
            SamplingPercentageEstimatorSettings settings = new SamplingPercentageEstimatorSettings
            {
                MaxSamplingPercentage = 13
            };
            AdaptiveSamplingPercentageEvaluatedCallback callback = (second, percentage, samplingPercentage, changed, estimatorSettings) => { };

            var tc = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);
            channelBuilder.UseAdaptiveSampling(settings, callback, "request");
            channelBuilder.Build();

            Assert.Equal(13, ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).MaxSamplingPercentage);
            Assert.Equal("request", ((AdaptiveSamplingTelemetryProcessor)tc.TelemetryProcessorChain.FirstTelemetryProcessor).ExcludedTypes);
        }
    }
}
