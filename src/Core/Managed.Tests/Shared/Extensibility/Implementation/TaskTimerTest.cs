namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
#if CORE_PCL || NET45 || WINRT
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET35 || NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class TaskTimerTest
    {
        [TestClass]
        public class Delay
        {
            [TestMethod]
            public void DefaultValueIsOneMinuteBecauseItHasToBeSomethingValid()
            {
                var timer = new TaskTimer();
                Assert.Equal(TimeSpan.FromMinutes(1), timer.Delay);
            }

            [TestMethod]
            public void CanBeChangedByConfigurableChannelComponents()
            {
                var timer = new TaskTimer();
                timer.Delay = TimeSpan.FromSeconds(42);
                Assert.Equal(42, timer.Delay.TotalSeconds);
            }

            [TestMethod]
            public void CanBeSetToInfiniteToPreventTimerFromFiring()
            {
                var timer = new TaskTimer();
                timer.Delay = new TimeSpan(0, 0, 0, 0, -1);
                Assert.Equal(new TimeSpan(0, 0, 0, 0, -1), timer.Delay);
            }

            [TestMethod]
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueIsZeroOrLess()
            {
                var timer = new TaskTimer();
                Assert.Throws<ArgumentOutOfRangeException>(() => timer.Delay = TimeSpan.Zero);
            }

            [TestMethod]
            public void ThrowsArgumentOutOfRangeExceptionWhenNewValueIsMoreThanMaxIntMilliseconds()
            {
                var timer = new TaskTimer();
                Assert.Throws<ArgumentOutOfRangeException>(() => timer.Delay = TimeSpan.FromMilliseconds((double)int.MaxValue + 1));
            }
        }

        [TestClass]
        public class IsStarted
        {
            [TestMethod]
            public void ReturnsFalseIfTimerWasNeverStarted()
            {
                var timer = new TaskTimer();
                Assert.False(timer.IsStarted);
            }

            [TestMethod]
            public void ReturnsTrueWhileUntilActionIsInvoked()
            {
                var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                var actionStarted = new ManualResetEventSlim();
                var actionCanFinish = new ManualResetEventSlim();
                timer.Start(
                    () => Task.Factory.StartNew(
                        () =>
                            {
                                actionStarted.Set();
                                actionCanFinish.Wait();
                            }));

                Assert.True(timer.IsStarted);

                actionStarted.Wait(50);

                Assert.False(timer.IsStarted);

                actionCanFinish.Set();
            }
        }

        [TestClass]
        public class Start
        {
            [TestMethod]
            public void InvokesActionAfterDelay()
            {
                var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                var actionInvoked = new ManualResetEventSlim();
                timer.Start(() => Task.Factory.StartNew(actionInvoked.Set));

                Assert.False(actionInvoked.IsSet);
                Assert.True(actionInvoked.Wait(50));
            }

            [TestMethod]
            public void CancelsPreviousActionWhenStartIsCalledMultipleTimes()
            {
                var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                int invokationCount = 0;
                var lastActionInvoked = new ManualResetEventSlim();
                timer.Start(() => Task.Factory.StartNew(() => Interlocked.Increment(ref invokationCount)));
                timer.Start(
                    () => Task.Factory.StartNew(
                        () =>
                            {
                                Interlocked.Increment(ref invokationCount);
                                lastActionInvoked.Set();
                            }));

                Assert.True(lastActionInvoked.Wait(50));
                Assert.Equal(1, invokationCount);
            }

#if !Wp80
            [TestMethod]
            [Timeout(1000)]
            public void HandlesAsyncExceptionThrownByTheDelegate()
            {
                TaskTimer timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                TestEventListener listener = new TestEventListener();
                listener.EnableEvents(CoreEventSource.Log, EventLevel.LogAlways);

                timer.Start(() => Task.Factory.StartNew(() => { throw new Exception(); }));

                Assert.NotNull(listener.Messages.FirstOrDefault());
            }

            [TestMethod]
            [Timeout(1000)]
            public void HandlesSyncExceptionThrownByTheDelegate()
            {
                TaskTimer timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };

                using (TestEventListener listener = new TestEventListener())
                {
                    listener.EnableEvents(CoreEventSource.Log, EventLevel.LogAlways);
                    timer.Start(() => { throw new Exception(); });

                    Assert.NotNull(listener.Messages.FirstOrDefault());
                }
            }
#endif
        }

        [TestClass]
        public class Cancel
        {
            [TestMethod]
            public void AbortsPreviousAction()
            {
                AsyncTest.Run(async () =>
                {
                    var timer = new TaskTimer { Delay = TimeSpan.FromMilliseconds(1) };
        
                    bool actionInvoked = false;
                    timer.Start(() => Task.Factory.StartNew(() => actionInvoked = true));
                    timer.Cancel();
        
                    await TaskEx.Delay(20);
        
                    Assert.False(actionInvoked);
                });
            }
        }
    }
}
