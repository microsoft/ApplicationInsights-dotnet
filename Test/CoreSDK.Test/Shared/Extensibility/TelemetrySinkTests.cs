namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Shared.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
#if !NET40
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class TelemetrySinkTests
    {
        [TestMethod]
        public void CommonTelemetryProcessorsAreInvoked()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());

            var sentTelemetry = new List<ITelemetry>(1);
            var channel = new StubTelemetryChannel();
            channel.OnSend = (telemetry) => sentTelemetry.Add(telemetry);
            configuration.TelemetryChannel = channel;

            var chainBuilder = new TelemetryProcessorChainBuilder(configuration);
            configuration.TelemetryProcessorChainBuilder = chainBuilder;
            chainBuilder.Use((next) =>
            {
                var first = new StubTelemetryProcessor(next);
                first.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenByFirst", "true");
                return first;
            });
            chainBuilder.Use((next) =>
            {
                var second = new StubTelemetryProcessor(next);
                second.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenBySecond", "true");
                return second;
            });

            var client = new TelemetryClient(configuration);
            client.TrackTrace("t1");

            Assert.Equal(1, sentTelemetry.Count);
            Assert.True(sentTelemetry[0].Context.Properties.ContainsKey("SeenByFirst"));
            Assert.True(sentTelemetry[0].Context.Properties.ContainsKey("SeenBySecond"));
        }

        [TestMethod]
        public void SinkProcessorsAreInvoked()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());

            var sentTelemetry = new List<ITelemetry>(1);
            var channel = new StubTelemetryChannel();
            channel.OnSend = (telemetry) => sentTelemetry.Add(telemetry);
            configuration.TelemetryChannel = channel;

            var chainBuilder = new TelemetryProcessorChainBuilder(configuration, configuration.DefaultTelemetrySink);
            configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder = chainBuilder;
            chainBuilder.Use((next) =>
            {
                var first = new StubTelemetryProcessor(next);
                first.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenByFirst", "true");
                return first;
            });
            chainBuilder.Use((next) =>
            {
                var second = new StubTelemetryProcessor(next);
                second.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenBySecond", "true");
                return second;
            });

            var client = new TelemetryClient(configuration);
            client.TrackTrace("t1");

            Assert.False(configuration.TelemetryProcessors.OfType<StubTelemetryProcessor>().Any()); // Both processors belong to the sink, not to the common chain.
            Assert.Equal(1, sentTelemetry.Count);
            Assert.True(sentTelemetry[0].Context.Properties.ContainsKey("SeenByFirst"));
            Assert.True(sentTelemetry[0].Context.Properties.ContainsKey("SeenBySecond"));
        }

        [TestMethod]
        public void CommonAndSinkProcessorsAreInvoked()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());

            var sentTelemetry = new List<ITelemetry>(1);
            var channel = new StubTelemetryChannel();
            channel.OnSend = (telemetry) => sentTelemetry.Add(telemetry);
            configuration.TelemetryChannel = channel;

            var commonChainBuilder = new TelemetryProcessorChainBuilder(configuration);
            configuration.TelemetryProcessorChainBuilder = commonChainBuilder;
            commonChainBuilder.Use((next) =>
            {
                var commonProcessor = new StubTelemetryProcessor(next);
                commonProcessor.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenByCommonProcessor", "true");
                return commonProcessor;
            });

            var sinkChainBuilder = new TelemetryProcessorChainBuilder(configuration, configuration.DefaultTelemetrySink);
            configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder = sinkChainBuilder;
            sinkChainBuilder.Use((next) =>
            {
                var sinkProcessor = new StubTelemetryProcessor(next);
                sinkProcessor.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenBySinkProcessor", "true");
                return sinkProcessor;
            });

            var client = new TelemetryClient(configuration);
            client.TrackTrace("t1");

            Assert.Equal(1, sentTelemetry.Count);
            Assert.True(sentTelemetry[0].Context.Properties.ContainsKey("SeenByCommonProcessor"));
            Assert.True(sentTelemetry[0].Context.Properties.ContainsKey("SeenBySinkProcessor"));
        }

        [TestMethod]
        public void TelemetryIsDeliveredToMultipleSinks()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());

            var firstChannelTelemetry = new List<ITelemetry>();
            var firstChannel = new StubTelemetryChannel();
            firstChannel.OnSend = (telemetry) => firstChannelTelemetry.Add(telemetry);
            configuration.DefaultTelemetrySink.TelemetryChannel = firstChannel;
            var chainBuilder = new TelemetryProcessorChainBuilder(configuration);
            configuration.TelemetryProcessorChainBuilder = chainBuilder;

            var secondChannelTelemetry = new List<ITelemetry>();
            var secondChannel = new StubTelemetryChannel();
            secondChannel.OnSend = (telemetry) => secondChannelTelemetry.Add(telemetry);
            var secondSink = new TelemetrySink(configuration, secondChannel);
            configuration.TelemetrySinks.Add(secondSink);

            var thirdChannelTelemetry = new List<ITelemetry>();
            var thirdChannel = new StubTelemetryChannel();
            thirdChannel.OnSend = (telemetry) => thirdChannelTelemetry.Add(telemetry);
            var thirdSink = new TelemetrySink(configuration, thirdChannel);
            configuration.TelemetrySinks.Add(thirdSink);

            var client = new TelemetryClient(configuration);
            client.TrackTrace("t1");

            Assert.Equal(1, firstChannelTelemetry.Count);
            Assert.Equal("t1", ((TraceTelemetry)firstChannelTelemetry[0]).Message);
            Assert.Equal(1, secondChannelTelemetry.Count);
            Assert.Equal("t1", ((TraceTelemetry)secondChannelTelemetry[0]).Message);
            Assert.Equal(1, thirdChannelTelemetry.Count);
            Assert.Equal("t1", ((TraceTelemetry)thirdChannelTelemetry[0]).Message);
        }

        [TestMethod]
        public void MultipleSinkTelemetryProcessorsAreInvoked()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());

            var commonChainBuilder = new TelemetryProcessorChainBuilder(configuration);
            configuration.TelemetryProcessorChainBuilder = commonChainBuilder;
            commonChainBuilder.Use((next) =>
            {
                var commonProcessor = new StubTelemetryProcessor(next);
                commonProcessor.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenByCommonProcessor", "true");
                return commonProcessor;
            });

            var firstChannelTelemetry = new List<ITelemetry>();
            var firstChannel = new StubTelemetryChannel();
            firstChannel.OnSend = (telemetry) => firstChannelTelemetry.Add(telemetry);
            configuration.DefaultTelemetrySink.TelemetryChannel = firstChannel;

            var firstSinkChainBuilder = new TelemetryProcessorChainBuilder(configuration, configuration.DefaultTelemetrySink);
            firstSinkChainBuilder.Use((next) =>
            {
                var firstSinkTelemetryProcessor = new StubTelemetryProcessor(next);
                firstSinkTelemetryProcessor.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenByFirstSinkProcessor", "true");
                return firstSinkTelemetryProcessor;
            });
            configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder = firstSinkChainBuilder;

            var secondChannelTelemetry = new List<ITelemetry>();
            var secondChannel = new StubTelemetryChannel();
            secondChannel.OnSend = (telemetry) => secondChannelTelemetry.Add(telemetry);
            var secondSink = new TelemetrySink(configuration, secondChannel);
            configuration.TelemetrySinks.Add(secondSink);

            var secondSinkChainBuilder = new TelemetryProcessorChainBuilder(configuration, secondSink);
            secondSinkChainBuilder.Use((next) =>
            {
                var secondSinkTelemetryProcessor = new StubTelemetryProcessor(next);
                secondSinkTelemetryProcessor.OnProcess = (telemetry) => telemetry.Context.Properties.Add("SeenBySecondSinkProcessor", "true");
                return secondSinkTelemetryProcessor;
            });
            secondSink.TelemetryProcessorChainBuilder = secondSinkChainBuilder;

            var client = new TelemetryClient(configuration);
            client.TrackTrace("t1");

            Assert.Equal(1, firstChannelTelemetry.Count);
            Assert.True(firstChannelTelemetry[0].Context.Properties.ContainsKey("SeenByCommonProcessor"));
            Assert.True(firstChannelTelemetry[0].Context.Properties.ContainsKey("SeenByFirstSinkProcessor"));
            Assert.False(firstChannelTelemetry[0].Context.Properties.ContainsKey("SeenBySecondSinkProcessor"));

            Assert.Equal(1, secondChannelTelemetry.Count);
            Assert.True(secondChannelTelemetry[0].Context.Properties.ContainsKey("SeenByCommonProcessor"));
            Assert.False(secondChannelTelemetry[0].Context.Properties.ContainsKey("SeenByFirstSinkProcessor"));
            Assert.True(secondChannelTelemetry[0].Context.Properties.ContainsKey("SeenBySecondSinkProcessor"));
        }

        [TestMethod]
        public void ConfigurationDisposesAllSinks()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());
            var commonChainBuilder = new TelemetryProcessorChainBuilder(configuration);
            configuration.TelemetryProcessorChainBuilder = commonChainBuilder;

            var firstChannel = new StubTelemetryChannel();
            bool firstChannelDisposed = false;
            firstChannel.OnDispose = () => firstChannelDisposed = true;
            configuration.DefaultTelemetrySink.TelemetryChannel = firstChannel;
            var firstSinkChainBuilder = new TelemetryProcessorChainBuilder(configuration, configuration.DefaultTelemetrySink);
            bool firstSinkTelemetryProcessorDisposed = false;
            firstSinkChainBuilder.Use((next) =>
            {
                var firstSinkTelemetryProcessor = new StubTelemetryProcessor(next);
                firstSinkTelemetryProcessor.OnDispose = () => firstSinkTelemetryProcessorDisposed = true;
                return firstSinkTelemetryProcessor;
            });
            configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder = firstSinkChainBuilder;

            var secondChannel = new StubTelemetryChannel();
            bool secondChannelDisposed = false;
            secondChannel.OnDispose = () => secondChannelDisposed = true;
            var secondSink = new TelemetrySink(configuration, secondChannel);
            var secondSinkChainBuilder = new TelemetryProcessorChainBuilder(configuration, secondSink);
            bool secondSinkTelemetryProcessorDisposed = false;
            secondSinkChainBuilder.Use((next) =>
            {
                var secondSinkTelemetryProcessor = new StubTelemetryProcessor(next);
                secondSinkTelemetryProcessor.OnDispose = () => secondSinkTelemetryProcessorDisposed = true;
                return secondSinkTelemetryProcessor;
            });
            secondSink.TelemetryProcessorChainBuilder = secondSinkChainBuilder;
            configuration.TelemetrySinks.Add(secondSink);

            var client = new TelemetryClient(configuration);
            client.TrackTrace("t1");
            configuration.Dispose();

            // We expect the channels to not be disposed (because they were created externally to sinks), but the processors should be disposed.
            Assert.True(firstSinkTelemetryProcessorDisposed);
            Assert.True(secondSinkTelemetryProcessorDisposed);
            Assert.False(firstChannelDisposed);
            Assert.False(secondChannelDisposed);
        }
    }
}
