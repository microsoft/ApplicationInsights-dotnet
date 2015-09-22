namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
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
            TelemetryTypeSupportsSampling((channel) => channel.Send(new DependencyTelemetry()));
        }
        
        [TestMethod]
        public void EventTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((channel) => channel.Send(new EventTelemetry("event")));
        }
        
        [TestMethod]
        public void ExceptionTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((channel) => channel.Send(new ExceptionTelemetry(new Exception("exception"))));
        }
        
        [TestMethod]
        public void MetricTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling((channel) => channel.Send(new MetricTelemetry("metric", 1.0)));
        }
        
        [TestMethod]
        public void PageViewTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((channel) => channel.Send(new PageViewTelemetry("page")));
        }
        
        [TestMethod]
        public void PerformanceCounterTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling(
                (channel) => channel.Send(new PerformanceCounterTelemetry("category", "counter", "instance", 1.0)));
        }
        
        [TestMethod]
        public void RequestTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((channel) => channel.Send(new RequestTelemetry()));
        }
        
        [TestMethod]
        public void SessionStateTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling((channel) => channel.Send(new SessionStateTelemetry()));
        }
        
        [TestMethod]
        public void TraceTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling((channel) => channel.Send(new TraceTelemetry("my trace")));
        }
        
        private static void TelemetryTypeDoesNotSupportSampling(Action<ITelemetryChannel> sendAction)
        {
            const int ItemsToGenerate = 100;
            const int SamplingPercentage = 10;
            var sentTelemetry = new List<ITelemetry>();
            var client = CreateTelemetryClientWithSampling(sentTelemetry, SamplingPercentage);

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                sendAction.Invoke(client);
            }

            Assert.Equal(sentTelemetry.Count, ItemsToGenerate);
        }

        private static void TelemetryTypeSupportsSampling(Action<ITelemetryChannel> sendAction)
        {
            const int ItemsToGenerate = 100;
            const int SamplingPercentage = 10;
            var sentTelemetry = new List<ITelemetry>();
            var client = CreateTelemetryClientWithSampling(sentTelemetry, SamplingPercentage);

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                sendAction.Invoke(client);
            }

            Assert.NotNull(sentTelemetry[0] as ISupportSampling);
            Assert.True(sentTelemetry.Count > 0);
            Assert.True(sentTelemetry.Count < ItemsToGenerate);
            Assert.Equal(SamplingPercentage, ((ISupportSampling)sentTelemetry[0]).SamplingPercentage);
        }

        private static ITelemetryChannel CreateTelemetryClientWithSampling(IList<ITelemetry> sentTelemetry, double samplingPercentage)
        {
            var channelBuilder = new TelemetryChannelBuilder();
            channelBuilder
                .UseSampling(samplingPercentage)
                .Use((next) => new StubTelemetryProcessor(next) { OnProcess = (t) => sentTelemetry.Add(t) });

            return channelBuilder.Build();
        }
    }
}
