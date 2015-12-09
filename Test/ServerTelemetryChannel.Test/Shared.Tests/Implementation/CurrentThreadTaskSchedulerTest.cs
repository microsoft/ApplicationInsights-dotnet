namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System.Threading;
    using System.Threading.Tasks;
#if NET40 || NET45
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class CurrentThreadTaskSchedulerTest
    {
        [TestMethod]
        public void InstanceReturnsSingletonThatCanBeReusedToImprovePerformance()
        {
            TaskScheduler instance = CurrentThreadTaskScheduler.Instance;
            Assert.NotNull(instance);
            Assert.IsType(typeof(CurrentThreadTaskScheduler), instance);
        }

        [TestMethod]
        public void MaximumConcurrencyLevelReturnsOneBecauseSchedulerExecutesTasksSynchronously()
        {
            var scheduler = new CurrentThreadTaskScheduler();
            Assert.Equal(1, scheduler.MaximumConcurrencyLevel);
        }

        [TestMethod]
        public void QueueTaskExecutesTaskSynchronously()
        {
            int taskThreadId = 0;
            var task = new Task(() => { taskThreadId = Thread.CurrentThread.ManagedThreadId; });

            task.Start(new CurrentThreadTaskScheduler());

            Assert.Equal(Thread.CurrentThread.ManagedThreadId, taskThreadId);
        }

        [TestMethod]
        public void TryExecuteTaskInlineExecutesTaskSynchronously()
        {
            int taskThreadId = 0;
            var task = new Task(() => { taskThreadId = Thread.CurrentThread.ManagedThreadId; });

            task.RunSynchronously(new CurrentThreadTaskScheduler());

            Assert.Equal(Thread.CurrentThread.ManagedThreadId, taskThreadId);
        }
    }
}
