namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    using ITelemetry = Microsoft.ApplicationInsights.Channel.ITelemetry;

#if NET45
    using TaskEx = System.Threading.Tasks.Task;
    
#endif

    public class TelemetryBufferTest
    {
        [TestClass]
        public class Class : TelemetryBufferTest
        {
            [TestMethod]
            public void ImplementsIEnumerableToAllowInspectingBufferContentsInTests()
            {
                TelemetryBuffer instance = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                Assert.True(instance is IEnumerable<ITelemetry>);
            }

            [TestMethod]
            public void ConstructorThrowsArgumentNullExceptionWhenSerializerIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new TelemetryBuffer(null, new StubApplicationLifecycle()));
            }

            [TestMethod]
            public void ConstructorThrowsArgumentNullExceptionWhenApplicationLifecycleIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new TelemetryBuffer(new StubTelemetrySerializer(), null));
            }
        }

        [TestClass]
        public class MaxTransmissionDelay
        {
            [TestMethod]
            public void DefaultValueIsAppropriateForProductionEnvironmentAndUnitTests()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                Assert.Equal(TimeSpan.FromSeconds(30), buffer.MaxTransmissionDelay);
            }

            [TestMethod]
            public void CanBeChangedByChannelToTunePerformance()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());

                var expectedValue = TimeSpan.FromSeconds(42);
                buffer.MaxTransmissionDelay = expectedValue;

                Assert.Equal(expectedValue, buffer.MaxTransmissionDelay);
            }
        }

        [TestClass]
        public class MaxNumberOfItemsPerTransmission : TelemetryBufferTest
        {
            [TestMethod]
            public void DefaultValueIsAppropriateForProductionEnvironmentAndUnitTests()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                Assert.Equal(500, buffer.Capacity);
                Assert.Equal(1000000, buffer.BacklogSize);
            }

            [TestMethod]
            public void CanBeSetByChannelToTunePerformance()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                buffer.Capacity = 42;
                buffer.BacklogSize = 3900;
                Assert.Equal(42, buffer.Capacity);
                Assert.Equal(3900, buffer.BacklogSize);
            }

            [TestMethod]
            public void ThrowsArgumentExceptionWhenBacklogSizeIsLowerThanCapacity()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                buffer.Capacity = 1111;
                Assert.Throws<ArgumentException>(() => buffer.BacklogSize = 1110);

                buffer.BacklogSize = 8000;
                Assert.Throws<ArgumentException>(() => buffer.Capacity = 8001);

            }

            [TestMethod]
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueIsLessThanMinimum()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Capacity = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.BacklogSize = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.BacklogSize = 1000); // 1001 is minimum anything low would throw.

                bool exceptionThrown = false;
                try
                {
                    buffer.BacklogSize = 1001; // 1001 is valid and should not throw.
                }
                catch(Exception)
                {
                    exceptionThrown = true;
                }

                Assert.True(exceptionThrown == false, "No exception should be thrown when trying to set backlogsize to 1001");                
            }
        }

        // TODO: Test that TelemetryBuffer.Send synchronously clears the buffer to prevent item # 501 from flushing again
        [TestClass]
        public class Send : TelemetryBufferTest
        {
            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenTelemetryIsNull()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                Assert.Throws<ArgumentNullException>(() => buffer.Process((ITelemetry)null));
            }

            [TestMethod]
            public void AddsTelemetryToBufferUntilItReachesMax()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                buffer.Capacity = 42;

                buffer.Process(new StubTelemetry());

                Assert.Equal(1, buffer.Count());
            }

            [TestMethod]
            public void TelemetryBufferDoesNotGrowBeyondMaxBacklogSize()
            {
                //TelemetryBufferWhichDoesNothingOnFlush does not flush items on buffer full,to test ItemDrop scenario.
                var buffer = new TelemetryBufferWhichDoesNothingOnFlush( new StubTelemetrySerializer(), new StubApplicationLifecycle() );
                buffer.Capacity = 2;
                buffer.BacklogSize = 1002;

                buffer.Process(new StubTelemetry());

                // Add more items (1005) to buffer than the max backlog size(1002)
                for (int i = 0; i < 1005; i++)
                {
                    buffer.Process(new StubTelemetry());
                }
                                
                // validate that items are not added after maxunsentbacklogsize is reached.
                // this also validate that items can still be added after Capacity is reached as it is only a soft limit.
                int bufferItemCount = buffer.Count();
                Assert.Equal(1002, bufferItemCount);
            }

            [TestMethod]
            public void FlushesBufferWhenNumberOfTelemetryItemsReachesMax()
            {
                var bufferFlushed = new ManualResetEventSlim();
                IEnumerable<ITelemetry> flushedTelemetry = null;
                var serializer = new StubTelemetrySerializer
                {
                    OnSerialize = telemetry =>
                    {
                        flushedTelemetry = telemetry.ToList();
                        bufferFlushed.Set();
                    },
                };

                var telemetryBuffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());

                var sentTelemetry = new List<ITelemetry> { new StubTelemetry(), new StubTelemetry() };
                telemetryBuffer.Capacity = sentTelemetry.Count;
                foreach (ITelemetry item in sentTelemetry)
                {
                    telemetryBuffer.Process(item);
                }

                Assert.True(bufferFlushed.Wait(30));
                Assert.Equal(sentTelemetry, flushedTelemetry);
            }

            [TestMethod]
            public void WaitsUntilTelemetryBufferIsSafeToModify()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                buffer.Capacity = 1;

                Task anotherThread;
                lock (buffer)
                {
                    anotherThread = TaskEx.Run(() => buffer.Process(new StubTelemetry()));
                    Assert.False(anotherThread.Wait(10));
                }

                Assert.True(anotherThread.Wait(50));
            }

            [TestMethod]
            public void StartsTimerThatFlushesBufferAfterMaxTransmissionDelay()
            {
                var telemetrySerialized = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry => 
                { 
                    telemetrySerialized.Set();
                };

                var buffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());

                buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(1);
                buffer.Process(new StubTelemetry());

                Assert.True(telemetrySerialized.Wait(50));
            }

            [TestMethod]
            public void DoesNotCancelPreviousFlush()
            {
                var telemetrySerialized = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry =>
                {
                    telemetrySerialized.Set();
                };
                var buffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());

                buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(1);
                buffer.Process(new StubTelemetry());

                buffer.MaxTransmissionDelay = TimeSpan.FromSeconds(42);
                buffer.Process(new StubTelemetry());

                Assert.True(telemetrySerialized.Wait(TimeSpan.FromMilliseconds(100)));
            }
        }

        [TestClass]
        public class FlushAsync : TelemetryBufferTest
        {
            [TestMethod]
            [Timeout(10000)]
            public void DoesntSerializeTelemetryIfBufferIsEmpty()
            {
                bool telemetrySerialized = false;
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry =>
                {
                    telemetrySerialized = true;
                };
                var telemetryBuffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());

                telemetryBuffer.FlushAsync().GetAwaiter().GetResult();
        
                Assert.False(telemetrySerialized);
            }

            [TestMethod]
            [Timeout(10000)]
            public void SerializesTelemetryIfBufferIsNotEmpty()
            {
                List<ITelemetry> serializedTelemetry = null;
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry => 
                {
                    serializedTelemetry = new List<ITelemetry>(telemetry);
                };

                var telemetryBuffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());
        
                var expectedTelemetry = new StubTelemetry();
                telemetryBuffer.Process(expectedTelemetry);
        
                telemetryBuffer.FlushAsync().GetAwaiter().GetResult();
        
                Assert.Same(expectedTelemetry, serializedTelemetry.Single());
            }

            [TestMethod]
            public void SerializesBufferOnThreadPoolToFreeUpCustomersThread()
            {
                int serializerThreadId = -1;
                var serializerInvoked = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry =>
                {
                    serializerThreadId = Thread.CurrentThread.ManagedThreadId;
                    serializerInvoked.Set();
                };

                var telemetryBuffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());
                telemetryBuffer.Process(new StubTelemetry());

                Task dontWait = telemetryBuffer.FlushAsync();

                Assert.True(serializerInvoked.Wait(100));
                Assert.NotEqual(serializerThreadId, Thread.CurrentThread.ManagedThreadId);
            }

            [TestMethod]
            [Timeout(10000)]
            public void EmptiesBufferAfterSerialization()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                buffer.Capacity = 10;
                buffer.Process(new StubTelemetry());

                buffer.FlushAsync().GetAwaiter().GetResult();
        
                Assert.Equal(0, buffer.Count());
            }

            [TestMethod]
            public void WaitsUntilTelemetryBufferIsSafeToModify()
            {
                var telemetryBuffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                telemetryBuffer.Process(new StubTelemetry());

                Task anotherThread;
                lock (telemetryBuffer)
                {
                    anotherThread = TaskEx.Run(() => telemetryBuffer.FlushAsync());
                    Assert.False(anotherThread.Wait(10));
                }

                Assert.True(anotherThread.Wait(50));
            }

            [TestMethod]
            [Timeout(10000)]
            public void CancelsPreviouslyStartedAutomaticFlushToPreventPreventPrematureTransmission()
            {
                var serializer = new StubTelemetrySerializer();
                var buffer = new TelemetryBuffer(serializer, new StubApplicationLifecycle());
        
                buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(1);
                buffer.Process(new StubTelemetry());
        
                buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(100);
                buffer.FlushAsync().Wait();
        
                var autoFlushed = new ManualResetEventSlim();
                serializer.OnSerialize = telemetry =>
                {
                    autoFlushed.Set();
                };
                buffer.Process(new StubTelemetry());
        
                Assert.False(autoFlushed.Wait(30));
            }

            [TestMethod]
            [Timeout(10000)]
            public void DoesNotContinueOnCapturedSynchronizationContextToImprovePerformance()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), new StubApplicationLifecycle());
                buffer.Process(new StubTelemetry());
        
                bool postedBack = false;
                using (var context = new StubSynchronizationContext())
                {
                    context.OnPost = (callback, state) =>
                    {
                        postedBack = true;
                        callback(state);
                    };

                    buffer.FlushAsync().GetAwaiter().GetResult();
        
                    Assert.False(postedBack);
                }
            }
        }

        [TestClass]
        public class HandleApplicationStoppingEvent : TelemetryBufferTest
        {
            [TestMethod]
            public void FlushesBufferToPreventLossOfTelemetry()
            {
                var applicationLifecycle = new StubApplicationLifecycle();
                var telemetrySerialized = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry => 
                {
                    telemetrySerialized.Set();
                };
                var buffer = new TelemetryBuffer(serializer, applicationLifecycle);
                buffer.Process(new StubTelemetry());

                applicationLifecycle.OnStopping(ApplicationStoppingEventArgs.Empty);

                Assert.True(telemetrySerialized.Wait(50));
            }

            [TestMethod]
            public void PreventsOperatingSystemFromSuspendingAsynchronousOperations()
            {
                var applicationLifecycle = new StubApplicationLifecycle();
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer(), applicationLifecycle);
                buffer.Process(new StubTelemetry());

                bool deferralAcquired = false;
                Func<Func<Task>, Task> asyncTaskRunner = asyncMethod =>
                {
                    deferralAcquired = true;
                    return asyncMethod();
                };
                applicationLifecycle.OnStopping(new ApplicationStoppingEventArgs(asyncTaskRunner));

                Assert.True(deferralAcquired);
            }
        }
    }
}
