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
        public void TransmissionProcessorProcessThrowsWhenChannelIsNull()
        {

            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key", TelemetryChannel = null };
            Assert.Throws<InvalidOperationException>(() => new TransmissionProcessor(configuration).Process(new StubTelemetry()));
        }

        #endregion       
    }
}
