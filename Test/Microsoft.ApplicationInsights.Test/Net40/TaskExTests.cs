using System.Linq;

namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class TaskExTests
    {
        /// <summary>
        /// Tests the scenario if RethrowIsFaulted doesn't throw exception for not faulted task.
        /// </summary>
        [TestMethod]
        public void RethrowIfFaultedDoesntThrowIfNoExceptionOccured()
        {
            Task task = Task.Factory.StartNew(() => { });

            task.Wait();

            Assert.DoesNotThrow(() => task.RethrowIfFaulted());
        }

        /// <summary>
        /// Tests the scenario if RethrowIsFaulted throws exception for not completed task.
        /// </summary>
        [TestMethod]
        public void RethrowIfFaultedThrowsIfTaskIsNotCompleted()
        {
            Task task = TaskEx.Delay(TimeSpan.FromMilliseconds(100));

            Assert.Throws<ArgumentException>(() => task.RethrowIfFaulted());
        }

        /// <summary>
        /// Tests the scenario if RethrowIsFaulted throws exception for faulted task.
        /// </summary>
        [TestMethod]
        public void RethrowIfFaultedThrowsIfExceptionOccured()
        {
            Task task = Task.Factory.StartNew(() => { throw new Exception(); });

            try
            {
                task.Wait();
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Throws<AggregateException>(() => task.RethrowIfFaulted());
        }

        /// <summary>
        /// Tests the scenario if Delay with zero timeout completes the task.
        /// </summary>
        [TestMethod]
        public void DelayWithZeroTimeoutCompletesTask()
        {
            Task task = TaskEx.Delay(TimeSpan.Zero);
            task.Wait();

            Assert.Equal(task.Status, TaskStatus.RanToCompletion);
        }

        /// <summary>
        /// Tests the scenario if Delay with timeout completes task after timeout has elapsed.
        /// </summary>
        [TestMethod]
        public void DelayWaitsAtLeastTimeout()
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(50);
            DateTime time = DateTime.UtcNow;
            Task task = TaskEx.Delay(timeout);
            
            task.Wait();
            TimeSpan spent = DateTime.UtcNow - time;

            Assert.True(spent >= timeout);
        }

        /// <summary>
        /// Tests the scenario if Delay turns the task in Canceled state after CancellationTokenSource is signaled.
        /// </summary>
        [TestMethod]
        public void DelayWhithCancelationCancelsTask()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task task = TaskEx.Delay(timeout, cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            Assert.True(task.IsCanceled);
        }

        /// <summary>
        /// Tests the scenario if Delay task throws on wait after cancellation.
        /// </summary>
        [TestMethod]
        public void DelayWhichIsCanceledThrows()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task task = TaskEx.Delay(timeout, cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            AggregateException aggregateException = Assert.Throws<AggregateException>(() => task.Wait());
            Assert.True(aggregateException.InnerExceptions.Single() is TaskCanceledException);
        }

        /// <summary>
        /// Tests the scenario if Delay task completes after cancellation without waiting for timeout.
        /// </summary>
        [TestMethod]
        public void DelayWith1SecondAndCanceledDoesntWaitForASecond()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task task = TaskEx.Delay(timeout, cancellationTokenSource.Token);

            DateTime time = DateTime.UtcNow;
            TimeSpan cancelTimeout = TimeSpan.FromMilliseconds(50);
            TaskEx.Delay(cancelTimeout).ContinueWith(task1 => cancellationTokenSource.Cancel());
            try
            {
                task.Wait();
            }
            catch (Exception)
            {
                // ignore
            }

            TimeSpan spent = DateTime.UtcNow - time;

            Assert.True(spent >= cancelTimeout && spent < timeout);
        }

        /// <summary>
        /// Tests the scenario if FromResult sets provided object as task result.
        /// </summary>
        [TestMethod]
        public void FromResultReturnsInputAsResult()
        {
            object result = new object();
            Task<object> task = TaskEx.FromResult<object>(result);

            task.Wait();

            Assert.True(result == task.Result);
        }

        /// <summary>
        /// Tests the scenario if WhenAny throws exception for empty tasks array.
        /// </summary>
        [TestMethod]
        public void WhenAnyThrowsForNoTasks()
        {
            Assert.Throws<ArgumentException>(() => { TaskEx.WhenAny(); });
        }

        /// <summary>
        /// Tests the scenario if WhenAny returns first task if it has completed faster.
        /// </summary>
        [TestMethod]
        public void WhenAnyReturnsFirstCompletedTask()
        {
            Task task1 = TaskEx.Delay(TimeSpan.FromMilliseconds(50));
            Task task2 = TaskEx.Delay(TimeSpan.FromMilliseconds(500));

            Task<Task> completedTask = TaskEx.WhenAny(task1, task2);
            completedTask.Wait();

            Assert.True(task1 == completedTask.Result);
        }

        /// <summary>
        /// Tests the scenario if WhenAny returns second task if it has completed faster.
        /// </summary>
        [TestMethod]
        public void WhenAnyReturnsFirstCompletedTask2()
        {
            Task task1 = TaskEx.Delay(TimeSpan.FromMilliseconds(100));
            Task task2 = TaskEx.Delay(TimeSpan.FromMilliseconds(50));

            Task<Task> completedTask = TaskEx.WhenAny(task1, task2);
            completedTask.Wait();

            Assert.True(task2 == completedTask.Result);
        }

        /// <summary>
        /// Tests the scenario if WhenAny returns first completed task even if it has been faulted.
        /// </summary>
        [TestMethod]
        public void WhenAnyReturnsFirstCompletedTaskEvenOnException()
        {
            Task task1 = TaskEx.Delay(TimeSpan.FromMilliseconds(100));
            Task task2 = TaskEx.Delay(TimeSpan.FromMilliseconds(50)).ContinueWith(task => { throw new Exception(); });

            Task<Task> completedTask = TaskEx.WhenAny(task1, task2);
            completedTask.Wait();

            Assert.True(task2 == completedTask.Result);
        }

        /// <summary>
        /// Tests the scenario if WhenAny returns first completed task even if it has been canceled.
        /// </summary>
        [TestMethod]
        public void WhenAnyReturnsFirstCompletedTaskEvenOnCanceled()
        {
            Task task1 = TaskEx.Delay(TimeSpan.FromMilliseconds(100));
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(10));
            Task task2 = TaskEx.Delay(TimeSpan.FromMilliseconds(50), cts.Token);

            Task<Task> completedTask = TaskEx.WhenAny(task1, task2);
            completedTask.Wait();

            Assert.True(task2 == completedTask.Result);
        }
    }
}