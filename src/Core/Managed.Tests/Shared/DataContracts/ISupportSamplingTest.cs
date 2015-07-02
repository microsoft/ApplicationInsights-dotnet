namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

    internal class ISupportSamplingTest<TTelemetry, TEndpointData>
        where TTelemetry : ITelemetry, new()
        where TEndpointData : Domain
    {
        public void Run()
        {
            this.TelemetryImplementsISupportSamplingInterface();
            this.TestTelemetryHasCorrectValueOfSamplingPercentageAfterTrack();
            this.TestTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization();
        }

        private void TelemetryImplementsISupportSamplingInterface()
        {
            var telemetry = new TTelemetry();

            Assert.IsNotNull(telemetry as ISupportSampling);
        }

        private void TestTelemetryHasCorrectValueOfSamplingPercentageAfterTrack()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            do
            {
                client.Track(new TTelemetry());
            }
            while (sentTelemetry.Count == 0);

            var samplingSupportingTelemetry = sentTelemetry[0] as ISupportSampling;

            Assert.IsNotNull(samplingSupportingTelemetry);
            Assert.AreEqual(10, samplingSupportingTelemetry.SamplingPercentage);
        }

        private void TestTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            do
            {
                client.Track(new TTelemetry());
            }
            while (sentTelemetry.Count == 0);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<EventTelemetry, TEndpointData>(sentTelemetry[0]);

            Assert.AreEqual(10, item.SampleRate);
        }
    }
}