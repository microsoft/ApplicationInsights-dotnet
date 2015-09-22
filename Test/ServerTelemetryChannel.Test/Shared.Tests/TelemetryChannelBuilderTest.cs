namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryChannelBuilderTest
    {
        [TestMethod]
        public void ThrowsInvalidOperationExceptionOnReturningNullFromUse()
        {
            var builder = new TelemetryChannelBuilder();
            builder.Use((next) => null);

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [TestMethod]
        public void TelemetryBufferIsUsedWhenNoOtherTelemetryProcessorsAreConfigured()
        {
            var builder = new TelemetryChannelBuilder();

            ITelemetryChannel channel = builder.Build();

            Assert.IsType<TelemetryBuffer>(((ServerTelemetryChannel)channel).TelemetryProcessor);
        }

        [TestMethod]
        public void UsesTelemetryProcessorGivenInUseToBuild()
        {
            var builder = new TelemetryChannelBuilder();
            builder.Use((next) => new StubTelemetryProcessor(next));

            ITelemetryChannel channel = builder.Build();

            Assert.IsType<StubTelemetryProcessor>(((ServerTelemetryChannel)channel).TelemetryProcessor);
        }
        
        [TestMethod]
        public void UseSamplingProcessorUsesSamplingProcessorToBuild()
        {
            var builder = new TelemetryChannelBuilder();
            builder.UseSampling(10.0);

            ServerTelemetryChannel channel = (ServerTelemetryChannel)builder.Build();

            Assert.IsType<SamplingTelemetryProcessor>(channel.TelemetryProcessor);
            Assert.Equal(10, ((SamplingTelemetryProcessor)channel.TelemetryProcessor).SamplingPercentage);
        }

        [TestMethod]
        public void BuildUsesTelemetryProcesorFactoryOnEachCall()
        {
            var builder = new TelemetryChannelBuilder();
            builder.Use((next) => new StubTelemetryProcessor(next));

            var channel1 = builder.Build();
            var channel2 = builder.Build();

            Assert.NotSame(channel1, channel2);
        }

        [TestMethod]
        public void BuildOrdersTelemetryChannelsInOrderOfUseCalls()
        {
            StringBuilder outputCollector = new StringBuilder();
            var builder = new TelemetryChannelBuilder();
            builder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (item) => { outputCollector.Append("processor1"); } });
            builder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (item) => { outputCollector.Append("processor2"); } });
            ITelemetryChannel channel = builder.Build();

            channel.Send(new StubTelemetry());

            Assert.Equal("processor1processor2", outputCollector.ToString());
        }
    }
}
