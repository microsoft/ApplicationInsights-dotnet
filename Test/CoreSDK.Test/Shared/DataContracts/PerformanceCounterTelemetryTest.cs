namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

    [TestClass]
    public class PerformanceCounterTelemetryTest
    {
        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            PerformanceCounterTelemetry original = new PerformanceCounterTelemetry();
            original.CategoryName = null;
            original.CounterName = null;
            original.InstanceName = null;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<PerformanceCounterTelemetry, DataPlatformModel.PerformanceCounterData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }

        [TestMethod]
        public void PerformanceCounterTelemetryIsNotSubjectToSampling()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            const int ItemsToGenerate = 100;

            for (int i = 0; i < 100; i++)
            {
                client.Track(new PerformanceCounterTelemetry("category", "counter", "instance", 1.0));
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }

        /// <summary>
        /// For some reason DataPlatformModel.PerformanceCounterData does not derive from Domain 
        /// type and this test cannot have the same structure as on all other sampling 
        /// supporting telemetry items. 
        /// May be corrected in the future.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void PerformanceCounterTelemetryImplementsISupportSamplingContract()
        {
            // var test = new ISupportSamplingTest<PerformanceCounterTelemetry, DataPlatformModel.PerformanceCounterData>();
            // test.Run();
        }
    }
}
