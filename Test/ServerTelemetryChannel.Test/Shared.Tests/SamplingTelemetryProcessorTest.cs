namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;    
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    
    [TestClass]
    public class SamplingTelemetryProcessorTest
    {
        [TestMethod]
        public void ThrowsAgrumentNullExceptionWithoutNextPocessor()
        {
            Assert.Throws<ArgumentNullException>(() => new SamplingTelemetryProcessor(null));
        }

        [TestMethod]
        public void DefaultSamplingRateIs100Percent()
        {
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null));

            Assert.Equal(processor.SamplingPercentage, 100.0, 12);
        }

        [TestMethod]
        public void AllTelemetryIsSentWithDefaultSamplingRate()
        {
            var sentTelemetry = new List<ITelemetry>();
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null) { OnProcess = t => sentTelemetry.Add(t) });

            const int ItemsToGenerate = 100;

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                processor.Process(new RequestTelemetry());
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }

        [TestMethod]
        public void TelemetryItemHasSamplingPercentageSet()
        {
            var sentTelemetry = new List<ITelemetry>();
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null) { OnProcess = t => sentTelemetry.Add(t) });
            processor.SamplingPercentage = 20;

            do
            {
                processor.Process(new RequestTelemetry());
            }
            while (sentTelemetry.Count == 0);

            Assert.Equal(20, ((ISupportSampling)sentTelemetry[0]).SamplingPercentage);
        }
        
        [TestMethod]
        public void DependencyTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((telemetryProcessors) => telemetryProcessors.Process(new DependencyTelemetry()));
        }
        
        [TestMethod]
        public void EventTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((telemetryProcessors) => telemetryProcessors.Process(new EventTelemetry("event")));
        }
        
        [TestMethod]
        public void ExceptionTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((telemetryProcessors) => telemetryProcessors.Process(new ExceptionTelemetry(new Exception("exception"))));
        }
        
        [TestMethod]
        public void MetricTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling((telemetryProcessors) => telemetryProcessors.Process(new MetricTelemetry("metric", 1.0)));
        }
        
        [TestMethod]
        public void PageViewTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((telemetryProcessors) => telemetryProcessors.Process(new PageViewTelemetry("page")));
        }
        
        [TestMethod]
        public void PerformanceCounterTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling(
                (telemetryProcessors) => telemetryProcessors.Process(new PerformanceCounterTelemetry("category", "counter", "instance", 1.0)));
        }
        
        [TestMethod]
        public void RequestTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((telemetryProcessors) => telemetryProcessors.Process(new RequestTelemetry()));
        }
        
        [TestMethod]
        public void SessionStateTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling((telemetryProcessors) => telemetryProcessors.Process(new SessionStateTelemetry()));
        }
        
        [TestMethod]
        public void TraceTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((telemetryProcessors) => telemetryProcessors.Process(new TraceTelemetry("my trace")));
        }
        
        private static void TelemetryTypeDoesNotSupportSampling(Action<TelemetryProcessorChain> sendAction)
        {
            const int ItemsToGenerate = 100;
            const int SamplingPercentage = 10;
            var sentTelemetry = new List<ITelemetry>();
            var telemetryProcessorChainWithSampling = CreateTelemetryProcessorChainWithSampling(sentTelemetry, SamplingPercentage);

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                sendAction.Invoke(telemetryProcessorChainWithSampling);
            }

            Assert.Equal(sentTelemetry.Count, ItemsToGenerate);
        }

        private static void TelemetryTypeSupportsSampling(Action<TelemetryProcessorChain> sendAction)
        {
            const int ItemsToGenerate = 100;
            const int SamplingPercentage = 10;
            var sentTelemetry = new List<ITelemetry>();
            var telemetryProcessorChainWithSampling = CreateTelemetryProcessorChainWithSampling(sentTelemetry, SamplingPercentage);

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                sendAction.Invoke(telemetryProcessorChainWithSampling);
            }

            Assert.NotNull(sentTelemetry[0] as ISupportSampling);
            Assert.True(sentTelemetry.Count > 0);
            Assert.True(sentTelemetry.Count < ItemsToGenerate);
            Assert.Equal(SamplingPercentage, ((ISupportSampling)sentTelemetry[0]).SamplingPercentage);
        }

        private static TelemetryProcessorChain CreateTelemetryProcessorChainWithSampling(IList<ITelemetry> sentTelemetry, double samplingPercentage)
        {
            var tc = new TelemetryConfiguration() {TelemetryChannel = new StubTelemetryChannel()};
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);            
            channelBuilder.UseSampling(samplingPercentage);
            channelBuilder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (t) => sentTelemetry.Add(t) });
            
            channelBuilder.Build();

            return tc.TelemetryProcessors;
        }
    }
}
