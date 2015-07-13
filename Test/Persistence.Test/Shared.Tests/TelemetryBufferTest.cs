namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET40 || NET45 || NET35
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

    public class TelemetryBufferTest
    {
        [TestClass]
        public class MaxNumberOfItemsPerTransmission : TelemetryBufferTest
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
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueIsLessThanOne()
            {
                var buffer = new TelemetryBuffer();
                Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Capacity = 0);
            }
        }

        // TODO: Test that TelemetryBuffer.Send synchronously clears the buffer to prevent item # 501 from flushing again
        /*[TestClass]
        public class Send : TelemetryBufferTest
        {
            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenTelemetryIsNull()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer());
                Assert.Throws<ArgumentNullException>(() => buffer.Send((ITelemetry)null));
            }

            [TestMethod]
            public void AddsTelemetryToBufferUntilItReachesMax()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer());
                buffer.Capacity = 42;

                buffer.Send(new StubTelemetry());

                Assert.Equal(1, buffer.Count());
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

                var telemetryBuffer = new TelemetryBuffer(serializer);

                var sentTelemetry = new List<ITelemetry> { new StubTelemetry(), new StubTelemetry() };
                telemetryBuffer.Capacity = sentTelemetry.Count;
                foreach (ITelemetry item in sentTelemetry)
                {
                    telemetryBuffer.Send(item);
                }

                Assert.True(bufferFlushed.Wait(30));
                Assert.Equal(sentTelemetry, flushedTelemetry);
            }

            [TestMethod]
            public void WaitsUntilTelemetryBufferIsSafeToModify()
            {
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer());
                buffer.Capacity = 1;

                Task anotherThread;
                lock (buffer)
                {
                    anotherThread = TaskEx.Run(() => buffer.Send(new StubTelemetry()));
                    Assert.False(anotherThread.Wait(10));
                }

                Assert.True(anotherThread.Wait(50));
            }

            [TestMethod]
            public void StartsTimerThatFlushesBufferAfterMaxTransmissionDelay()
            {
                var telemetrySerialized = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry => { telemetrySerialized.Set(); };

                var buffer = new TelemetryBuffer(serializer);

                buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(1);
                buffer.Send(new StubTelemetry());

                Assert.True(telemetrySerialized.Wait(50));
            }

            [TestMethod]
            public void DoesNotCancelPreviousFlush()
            {
                var telemetrySerialized = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer { OnSerialize = telemetry => telemetrySerialized.Set() };
                var buffer = new TelemetryBuffer(serializer);

                buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(1);
                buffer.Send(new StubTelemetry());

                buffer.MaxTransmissionDelay = TimeSpan.FromSeconds(42);
                buffer.Send(new StubTelemetry());

                Assert.True(telemetrySerialized.Wait(TimeSpan.FromMilliseconds(100)));
            }
        }

        [TestClass]
        public class FlushAsync : TelemetryBufferTest
        {
            [TestMethod]
            public void DoesntSerializeTelemetryIfBufferIsEmpty()
            {
                AsyncTest.Run(async () =>
                {
                    bool telemetrySerialized = false;
                    var serializer = new StubTelemetrySerializer { OnSerialize = telemetry => telemetrySerialized = true };
                    var telemetryBuffer = new TelemetryBuffer(serializer);
        
                    await telemetryBuffer.FlushAsync();
        
                    Assert.False(telemetrySerialized);
                });
            }

            [TestMethod]
            public void SerializesTelemetryIfBufferIsNotEmpty()
            {
                AsyncTest.Run(async () =>
                {
                    List<ITelemetry> serializedTelemetry = null;
                    var serializer = new StubTelemetrySerializer
                    {
                        OnSerialize = telemetry => serializedTelemetry = new List<ITelemetry>(telemetry)
                    };
        
                    var telemetryBuffer = new TelemetryBuffer(serializer);
        
                    var expectedTelemetry = new StubTelemetry();
                    telemetryBuffer.Send(expectedTelemetry);
        
                    await telemetryBuffer.FlushAsync();
        
                    Assert.Same(expectedTelemetry, serializedTelemetry.Single());
                });
            }

            [TestMethod]
            public void SerializesBufferOnThreadPoolToFreeUpCustomersThread()
            {
                int serializerThreadId = -1;
                var serializerInvoked = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer();
                serializer.OnSerialize = telemetry =>
                {
                    serializerThreadId = EnvironmentEx.CurrentManagedThreadId;
                    serializerInvoked.Set();
                };

                var telemetryBuffer = new TelemetryBuffer(serializer);
                telemetryBuffer.Send(new StubTelemetry());

                Task dontWait = telemetryBuffer.FlushAsync();

                Assert.True(serializerInvoked.Wait(100));
                Assert.NotEqual(serializerThreadId, EnvironmentEx.CurrentManagedThreadId);
            }

            [TestMethod]
            public void EmptiesBufferAfterSerialization()
            {
                AsyncTest.Run(async () =>
                {
                    var buffer = new TelemetryBuffer(new StubTelemetrySerializer());
                    buffer.Capacity = 10;
                    buffer.Send(new StubTelemetry());
        
                    await buffer.FlushAsync();
        
                    Assert.Equal(0, buffer.Count());
                });
            }

            [TestMethod]
            public void WaitsUntilTelemetryBufferIsSafeToModify()
            {
                var telemetryBuffer = new TelemetryBuffer(new StubTelemetrySerializer());
                telemetryBuffer.Send(new StubTelemetry());

                Task anotherThread;
                lock (telemetryBuffer)
                {
                    anotherThread = TaskEx.Run(() => telemetryBuffer.FlushAsync());
                    Assert.False(anotherThread.Wait(10));
                }

                Assert.True(anotherThread.Wait(50));
            }

            [TestMethod]
            public void CancelsPreviouslyStartedAutomaticFlushToPreventPreventPrematureTransmission()
            {
                AsyncTest.Run(async () =>
                {
                    var serializer = new StubTelemetrySerializer();
                    var buffer = new TelemetryBuffer(serializer);
        
                    buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(1);
                    buffer.Send(new StubTelemetry());
        
                    buffer.MaxTransmissionDelay = TimeSpan.FromMilliseconds(100);
                    await buffer.FlushAsync();
        
                    var autoFlushed = new ManualResetEventSlim();
                    serializer.OnSerialize = telemetry => autoFlushed.Set();
                    buffer.Send(new StubTelemetry());
        
                    Assert.False(autoFlushed.Wait(30));
                });
            }

            [TestMethod]
            public void DoesNotContinueOnCapturedSynchronizationContextToImprovePerformance()
            {
                AsyncTest.Run(async () =>
                {
                    var buffer = new TelemetryBuffer(new StubTelemetrySerializer());
                    buffer.Send(new StubTelemetry());
        
                    bool postedBack = false;
                    using (var context = new StubSynchronizationContext())
                    {
                        context.OnPost = (callback, state) =>
                        {
                            postedBack = true;
                            callback(state);
                        };
        
                        await buffer.FlushAsync().ConfigureAwait(false);
        
                        Assert.False(postedBack);
                    }
                });
            }
        }

        [TestClass]
        public class HandleApplicationStoppingEvent : TelemetryBufferTest
        {
            [TestCleanup]
            public void TestCleanup()
            {
                PlatformSingleton.Current = null;
                ApplicationLifecycle.Service = null;
            }

            [TestMethod]
            public void FlushesBufferToPreventLossOfTelemetry()
            {
                var platform = new StubPlatform();
                var applicationLifecycle = new StubApplicationLifecycle();
                PlatformSingleton.Current = platform;
                ApplicationLifecycle.SetProvider(applicationLifecycle);
                var telemetrySerialized = new ManualResetEventSlim();
                var serializer = new StubTelemetrySerializer
                {
                    OnSerialize = telemetry => telemetrySerialized.Set()
                };
                var buffer = new TelemetryBuffer(serializer);
                buffer.Send(new StubTelemetry());

                applicationLifecycle.OnStopping(ApplicationStoppingEventArgs.Empty);

                Assert.True(telemetrySerialized.Wait(50));
            }

            [TestMethod]
            public void PreventsOperatingSystemFromSuspendingAsynchronousOperations()
            {
                var platform = new StubPlatform();
                var applicationLifecycle = new StubApplicationLifecycle();
                PlatformSingleton.Current = platform;
                ApplicationLifecycle.SetProvider(applicationLifecycle);
                var buffer = new TelemetryBuffer(new StubTelemetrySerializer());
                buffer.Send(new StubTelemetry());

                bool deferralAcquired = false;
                Func<Func<Task>, Task> asyncTaskRunner = asyncMethod =>
                {
                    deferralAcquired = true;
                    return asyncMethod();
                };
                applicationLifecycle.OnStopping(new ApplicationStoppingEventArgs(asyncTaskRunner));

                Assert.True(deferralAcquired);
            }
        }*/
    }
}
