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
#if NET40 || NET45 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryBufferTest
    {
        [TestMethod]
        public void DefaultValueIsAppropriateForProductionEnvironmentAndUnitTests()
        {
            var buffer = new TelemetryBuffer();
            Assert.Equal(500, buffer.Capacity);
            Assert.Equal(1000000, buffer.BacklogSize);
        }

        [TestMethod]
        public void CanBeSetByChannelToTunePerformance()
        {
            var buffer = new TelemetryBuffer();
            buffer.Capacity = 42;
            buffer.BacklogSize = 9999;
            Assert.Equal(42, buffer.Capacity);
            Assert.Equal(9999, buffer.BacklogSize);
        }

        [TestMethod]
        public void WhenNewValueIsLessThanMinimumSetToDefault()
        {
            var buffer = new TelemetryBuffer();
            buffer.Capacity = 0;
            buffer.BacklogSize = 1000; //1001 is the minimum, setting to anything low should be overruled with minimum allowed.

            Assert.Equal(500,buffer.Capacity);
            Assert.Equal(1001, buffer.BacklogSize);
        }

        [TestMethod]
        public void MaxBacklogCannotBeBelowCapacity()
        {     
            var buffer = new TelemetryBuffer();
            buffer.Capacity = 9999; // a value greater than the minimum allowed for MaxBacklogSize            

            //Attempt to set MaxBacklogSize below Capacity
            buffer.BacklogSize = buffer.Capacity - 1;

            // Validate that MaximumBacklogSize will be set to Capacity
            Assert.Equal(buffer.Capacity, buffer.BacklogSize);
        }

        [TestMethod]
        public void CapacityCannotBeAboveBacklogSize()
        {

            var buffer = new TelemetryBuffer();            

            //Attempt to set Capacity above MaxBacklogSize
            buffer.Capacity = buffer.BacklogSize + 1;

            // Validate that Capacity will be set to MaxBacklogSize
            Assert.Equal(buffer.Capacity, buffer.BacklogSize);
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

        [TestMethod]
        public void TelemetryBufferDoesNotGrowBeyondMaxBacklogSize()
        {            
            TelemetryBuffer buffer = new TelemetryBuffer { Capacity = 2, BacklogSize = 1002};
            buffer.OnFull = () => { //intentionally blank to simulate situation where buffer
                                    //is not emptied.
                                  };

            // Add more items to buffer than the max backlog size
            for(int i = 0; i < 1005; i++)
            {
                buffer.Enqueue(new EventTelemetry("Event" + i));
            }
            
            

            // validate that items are not added after maxunsentbacklogsize is reached.
            // this also validate that items can still be added after Capacity is reached as it is only a soft limit.
            int bufferItemCount = buffer.Dequeue().Count();
            Assert.Equal(1002, bufferItemCount);

        }
    }
}
