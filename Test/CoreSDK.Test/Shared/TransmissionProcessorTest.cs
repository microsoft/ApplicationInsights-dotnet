namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET40 || NET45 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;
    using Extensibility.Implementation.Platform;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;
    [TestClass]
    public class TransmissionProcessorTest
    {
        #region Tests

        [TestMethod]
        public void TransmissionProcessorTransmitsAllDataWhenNoOtherProcessorPresent()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key", TelemetryChannel = channel };

            var client = new TelemetryClient(configuration);

            var transmissionProcessor = new TransmissionProcessor(configuration);

            const int ItemsToGenerate = 100;

            for (int i = 0; i < ItemsToGenerate; i++)
            {                
                transmissionProcessor.Process(new RequestTelemetry());
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }

        [TestMethod]
        public void TransmissionProcessorThrowsWhenNullConfigurationIsPassedToContructor()
        {
            Assert.Throws<ArgumentNullException>(() => new TransmissionProcessor(null));
        }

        [TestMethod]
        public void TransmissionProcessorStartsChannelSanitizationAfterDebugOutputSanitization()
        {
            var debugOutput = new StubDebugOutput
            {
                OnWriteLine = message =>
                {
                    // do nothing
                },
                OnIsAttached = () => true,
            };

            PlatformSingleton.Current = new StubPlatform { OnGetDebugOutput = () => debugOutput };

            var channel = new StubTelemetryChannel { OnSend = t => new Task(() =>
                 {
                     ((ITelemetry)t).Sanitize();
                 }).Start()
            };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key", TelemetryChannel = channel };

            var client = new TelemetryClient(configuration);

            var transmissionProcessor = new TransmissionProcessor(configuration);

            const int ItemsToGenerate = 100;
            Random random = new Random();

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                EventTelemetry telemetry = new EventTelemetry();

                int len = random.Next(50);

                for (int j = 0; j < len; j++)
                {
                    telemetry.Properties.Add(j.ToString(), j.ToString());
                }

                transmissionProcessor.Process(telemetry);
            }

            // There were a bug that causes Sanitize call from DebugOutput tracer conflict with Sanitize call from Channel
            // If no exceptions here - everything fine
            Assert.True(true);
        }

        #endregion       
    }
}
