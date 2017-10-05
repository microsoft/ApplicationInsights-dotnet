using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.Metrics.TestUtil
{
    internal static class Util
    {
        public static void AssertAreEqual<T>(T[] array1, T[] array2)
        {
            if (array1 == array2)
            {
                return;
            }

            Assert.IsNotNull(array1);
            Assert.IsNotNull(array2);

            Assert.AreEqual(array1.Length, array1.Length);

            for(int i = 0; i < array1.Length; i++)
            {
                Assert.AreEqual(array1[i], array2[i], message: $" at index {i}");
            }
        }

        public static bool AreEqual<T>(T[] array1, T[] array2)
        {
            if (array1 == array2)
            {
                return true;
            }

            if (array1 == null)
            {
                return false;
            }

            if (array2 == null)
            {
                return false;
            }

            if (array1.Length != array1.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] == null && array2[i] == null)
                {
                    continue;
                }

                if (array1 == null)
                {
                    return false;
                }

                if (array2 == null)
                {
                    return false;
                }

                if (! array1[i].Equals(array2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static TelemetryConfiguration CreateAITelemetryConfig(out IList<ITelemetry> telemetrySentToChannel)
        {
            StubTelemetryChannel channel = new StubTelemetryChannel();
            string iKey = Guid.NewGuid().ToString("D");
            TelemetryConfiguration telemetryConfig = new TelemetryConfiguration(iKey, channel);

            var channelBuilder = new TelemetryProcessorChainBuilder(telemetryConfig);
            channelBuilder.Build();

            foreach (ITelemetryProcessor initializer in telemetryConfig.TelemetryInitializers)
            {
                ITelemetryModule m = initializer as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            foreach (ITelemetryProcessor processor in telemetryConfig.TelemetryProcessors)
            {
                ITelemetryModule m = processor as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }

            telemetrySentToChannel = channel.TelemetryItems;
            return telemetryConfig;
        }
    }
}
