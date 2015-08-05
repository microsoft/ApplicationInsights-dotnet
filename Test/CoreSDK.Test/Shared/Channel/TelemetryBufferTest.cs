namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET40 || NET45 || NET35 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;
#if !NET35
    using EnvironmentEx = System.Environment;    
#endif
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class TelemetryBufferTest
    {
        [TestMethod]
        public void DefaultValueIsAppropriateForProductionEnvironmentAndUnitTests()
        {
            var buffer = new TelemetryBuffer();
            Assert.Equal(500, buffer.Capacity);
        }

        [TestMethod]
        public void CanBeSetByChannelToTunePerformance()
        {
            var buffer = new TelemetryBuffer();
            buffer.Capacity = 42;
            Assert.Equal(42, buffer.Capacity);
        }

        [TestMethod]
        public void WhenNewValueIsLessThanOneSetToDefault()
        {
            var buffer = new TelemetryBuffer();
            buffer.Capacity = 0;

            Assert.Equal(buffer.Capacity, 500);
        }

        [TestMethod]
        public void TelemetryBufferCallingOnFullActionWhenBufferCapacityReached()
        {
            IEnumerable<ITelemetry> items = null;
            TelemetryBuffer buffer = new TelemetryBuffer { Capacity = 2 };
            buffer.OnFull = () => { items = buffer.Dequeue(); };

            buffer.Enqueue(new EventTelemetry("Event1"));
            buffer.Enqueue(new EventTelemetry("Event2"));

            Assert.NotNull(items);
            Assert.Equal(2, items.Count());
        }
    }
}
