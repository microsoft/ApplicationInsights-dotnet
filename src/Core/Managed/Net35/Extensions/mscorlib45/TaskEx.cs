namespace System.Threading.Tasks
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// The task extensions.
    /// </summary>
    internal static class TaskEx
    {
        internal static TaskScheduler CapturedTaskScheduler
        {
            get
            {
                if (SynchronizationContext.Current == null)
                {
                    return TaskScheduler.Default;
                }

                return TaskScheduler.FromCurrentSynchronizationContext();
            }
        }

        internal static TaskScheduler DefaultTaskScheduler
        {
            get
            {
                return TaskScheduler.Default;
            }
        }
        
        public static ConfiguredTaskAwaiter GetAwaiter(this Task task)
        {
            return new ConfiguredTaskAwaiter(task, true);
        }

        public static ConfiguredTaskAwaiter<T> GetAwaiter<T>(this Task<T> task)
        {
            return new ConfiguredTaskAwaiter<T>(task, true);
        }

        /// <summary>
        /// Starts a Task that will complete after the specified due time.
        /// </summary>
        /// <param name="dueTime">The delay in milliseconds before the returned task completes.</param>
        /// <returns>
        /// The timed Task.
        /// </returns>
        public static Task Delay(int dueTime)
        {
            return TaskEx.Delay(dueTime, CancellationToken.None);
        }

        /// <summary>
        /// Starts a Task that will complete after the specified due time.
        /// </summary>
        /// <param name="dueTime">The delay before the returned task completes.</param>
        /// <returns>
        /// The timed Task.
        /// </returns>
        public static Task Delay(TimeSpan dueTime)
        {
            return TaskEx.Delay(dueTime, CancellationToken.None);
        }

        /// <summary>
        /// Starts a Task that will complete after the specified due time.
        /// </summary>
        /// <param name="dueTime">The delay before the returned task completes.</param><param name="cancellationToken">A CancellationToken that may be used to cancel the task before the due time occurs.</param>
        /// <returns>
        /// The timed Task.
        /// </returns>
        public static Task Delay(TimeSpan dueTime, CancellationToken cancellationToken)
        {
            long num = (long)dueTime.TotalMilliseconds;
            if (num < -1L || num > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }

            return TaskEx.Delay((int)num, cancellationToken);
        }

        /// <summary>
        /// Starts a Task that will complete after the specified due time.
        /// </summary>
        /// <param name="dueTime">The delay in milliseconds before the returned task completes.</param><param name="cancellationToken">A CancellationToken that may be used to cancel the task before the due time occurs.</param>
        /// <returns>
        /// The timed Task.
        /// </returns>
        public static Task Delay(int dueTime, CancellationToken cancellationToken)
        {
            if (dueTime < -1)
            {
                throw new ArgumentOutOfRangeException("dueTime", "The timeout must be non-negative or -1, and it must be less than or equal to Int32.MaxValue.");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new Task(() => { }, cancellationToken);
            }

            if (dueTime == 0)
            {
                return TaskEx.FromResult((object)null);
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration ctr = new CancellationTokenRegistration();
            Timer timer = null;
            timer = new Timer(
                state =>
                    {
                        ctr.Dispose();
                        timer.Dispose();
                        tcs.TrySetResult(true);
                    }, 
                null, 
                -1, 
                -1);

            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(
                    () =>
                        {
                            timer.Dispose();
                            tcs.TrySetCanceled();
                        });
            }

            timer.Change(dueTime, -1);
            return tcs.Task;
        }

        /// <summary>
        /// Creates an already completed <see cref="T:System.Threading.Tasks.Task`1"/> from the specified result.
        /// </summary>
        /// <param name="result">The result from which to create the completed task.</param>
        /// <returns>
        /// The completed task.
        /// </returns>
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>(result);
            completionSource.TrySetResult(result);
            return completionSource.Task;
        }

        /// <summary>
        /// Creates a Task that will complete only when all of the provided collection of Tasks has completed.
        /// </summary>
        /// <param name="tasks">The Tasks to monitor for completion.</param>
        /// <returns>
        /// A Task that represents the completion of all of the provided tasks.
        /// </returns>
        /// <remarks>
        /// If any of the provided Tasks faults, the returned Task will also fault, and its Exception will contain information
        ///             about all of the faulted tasks.  If no Tasks fault but one or more Tasks is canceled, the returned
        ///             Task will also be canceled.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks"/> argument is null.</exception><exception cref="T:System.ArgumentException">The <paramref name="tasks"/> argument contains a null reference.</exception>
        public static Task WhenAll(params Task[] tasks)
        {
            return TaskEx.WhenAll((IEnumerable<Task>)tasks);
        }

        /// <summary>
        /// Creates a Task that will complete only when all of the provided collection of Tasks has completed.
        /// </summary>
        /// <param name="tasks">The Tasks to monitor for completion.</param>
        /// <returns>
        /// A Task that represents the completion of all of the provided tasks.
        /// </returns>
        /// <remarks>
        /// If any of the provided Tasks faults, the returned Task will also fault, and its Exception will contain information
        ///             about all of the faulted tasks.  If no Tasks fault but one or more Tasks is canceled, the returned
        ///             Task will also be canceled.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks"/> argument is null.</exception><exception cref="T:System.ArgumentException">The <paramref name="tasks"/> argument contains a null reference.</exception>
        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            return TaskEx.WhenAll((IEnumerable<Task<TResult>>)tasks);
        }

        /// <summary>
        /// Creates a Task that will complete only when all of the provided collection of Tasks has completed.
        /// </summary>
        /// <param name="tasks">The Tasks to monitor for completion.</param>
        /// <returns>
        /// A Task that represents the completion of all of the provided tasks.
        /// </returns>
        /// <remarks>
        /// If any of the provided Tasks faults, the returned Task will also fault, and its Exception will contain information
        ///             about all of the faulted tasks.  If no Tasks fault but one or more Tasks is canceled, the returned
        ///             Task will also be canceled.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks"/> argument is null.</exception><exception cref="T:System.ArgumentException">The <paramref name="tasks"/> argument contains a null reference.</exception>
        public static Task WhenAll(IEnumerable<Task> tasks)
        {
            return TaskEx.WhenAllCore<object>(tasks, (completedTasks, tcs) => tcs.TrySetResult(null));
        }

        /// <summary>
        /// Creates a Task that will complete only when all of the provided collection of Tasks has completed.
        /// </summary>
        /// <param name="tasks">The Tasks to monitor for completion.</param>
        /// <returns>
        /// A Task that represents the completion of all of the provided tasks.
        /// </returns>
        /// <remarks>
        /// If any of the provided Tasks faults, the returned Task will also fault, and its Exception will contain information
        ///             about all of the faulted tasks.  If no Tasks fault but one or more Tasks is canceled, the returned
        ///             Task will also be canceled.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks"/> argument is null.</exception><exception cref="T:System.ArgumentException">The <paramref name="tasks"/> argument contains a null reference.</exception>
        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            return TaskEx.WhenAllCore<TResult[]>(tasks.Cast<Task>(), (completedTasks, tcs) => tcs.TrySetResult(completedTasks.Select(t => ((Task<TResult>)t).Result).ToArray()));
        }

        /// <summary>
        /// Creates a Task that will complete only when all of the provided collection of Tasks has completed.
        /// </summary>
        /// <param name="tasks">The Tasks to monitor for completion.</param><param name="setResultAction">A callback invoked when all of the tasks complete successfully in the RanToCompletion state.
        ///             This callback is responsible for storing the results into the TaskCompletionSource.
        ///             </param>
        /// <returns>
        /// A Task that represents the completion of all of the provided tasks.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="tasks"/> argument is null.</exception><exception cref="T:System.ArgumentException">The <paramref name="tasks"/> argument contains a null reference.</exception>
        private static Task<TResult> WhenAllCore<TResult>(IEnumerable<Task> tasks, Action<Task[], TaskCompletionSource<TResult>> setResultAction)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }

            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            Task[] taskArray = tasks as Task[] ?? tasks.ToArray();
            if (taskArray.Length == 0)
            {
                setResultAction(taskArray, tcs);
            }
            else
            {
                Task.Factory.ContinueWhenAll(
                    taskArray, 
                    completedTasks =>
                        {
                            List<Exception> exceptions = null;
                            bool isCancelled = false;
                            foreach (Task completedTask in completedTasks)
                            {
                                if (completedTask.IsFaulted)
                                {
                                    TaskEx.AddPotentiallyUnwrappedExceptions(ref exceptions, completedTask.Exception);
                                }
                                else if (completedTask.IsCanceled)
                                {
                                    isCancelled = true;
                                }
                            }

                            if (exceptions != null && exceptions.Count > 0)
                            {
                                tcs.TrySetException(exceptions);
                            }
                            else if (isCancelled)
                            {
                                tcs.TrySetCanceled();
                            }
                            else
                            {
                                setResultAction(completedTasks, tcs);
                            }
                        }, 
                        CancellationToken.None, 
                        TaskContinuationOptions.ExecuteSynchronously, 
                        TaskScheduler.Default);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Creates a Task that will complete when any of the tasks in the provided collection completes.
        /// </summary>
        /// <param name="tasks">The Tasks to be monitored.</param>
        /// <returns>
        /// A Task that represents the completion of any of the provided Tasks.  The completed Task is this Task's result.
        /// </returns>
        /// <remarks>
        /// Any Tasks that fault will need to have their exceptions observed elsewhere.
        /// </remarks>
        public static Task<Task> WhenAny(params Task[] tasks)
        {
            return TaskEx.WhenAny((IEnumerable<Task>)tasks);
        }

        /// <summary>
        /// Creates a Task that will complete when any of the tasks in the provided collection completes.
        /// </summary>
        /// <param name="tasks">The Tasks to be monitored.</param>
        /// <returns>
        /// A Task that represents the completion of any of the provided Tasks.  The completed Task is this Task's result.
        /// </returns>
        /// <remarks>
        /// Any Tasks that fault will need to have their exceptions observed elsewhere.
        /// </remarks>
        public static Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }

            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();
            Task.Factory.ContinueWhenAny(
                tasks.ToArray(),
                completed =>
                    {
                        tcs.TrySetResult(completed);
                    },
                CancellationToken.None, 
                TaskContinuationOptions.ExecuteSynchronously, 
                TaskScheduler.Default);
            return tcs.Task;
        }

        /// <summary>
        /// Creates a task that runs the specified function.
        /// </summary>
        /// <param name="function">The function to execute asynchronously.</param>
        /// <returns>
        /// A task that represents the completion of the action.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="function"/> argument is null.</exception>
        public static Task Run(Action function)
        {
            return TaskEx.Run(() => { function(); return (object)null; }, CancellationToken.None);
        }

        /// <summary>
        /// Creates a task that runs the specified function.
        /// </summary>
        /// <param name="function">The function to execute asynchronously.</param>
        /// <returns>
        /// A task that represents the completion of the action.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="function"/> argument is null.</exception>
        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            return TaskEx.Run(function, CancellationToken.None);
        }

        /// <summary>
        /// Creates a task that runs the specified function.
        /// </summary>
        /// <param name="function">The function to execute asynchronously.</param>
        /// <returns>
        /// A task that represents the completion of the action.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="function"/> argument is null.</exception>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
        {
            return TaskEx.Run(function, CancellationToken.None);
        }

        /// <summary>
        /// Creates a task that runs the specified function.
        /// </summary>
        /// <param name="function">The action to execute.</param><param name="cancellationToken">The CancellationToken to use to cancel the task.</param>
        /// <returns>
        /// A task that represents the completion of the action.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="function"/> argument is null.</exception>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            return TaskEx.Run<Task<TResult>>(function, cancellationToken).Unwrap();
        }

        /// <summary>
        /// Creates a task that runs the specified function.
        /// </summary>
        /// <param name="function">The action to execute.</param><param name="cancellationToken">The CancellationToken to use to cancel the task.</param>
        /// <returns>
        /// A task that represents the completion of the action.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="function"/> argument is null.</exception>
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(function, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
        }

        /// <summary>
        /// Adds the target exception to the list, initializing the list if it's null.
        /// </summary>
        /// <param name="targetList">The list to which to add the exception and initialize if the list is null.</param><param name="exception">The exception to add, and unwrap if it's an aggregate.</param>
        private static void AddPotentiallyUnwrappedExceptions(ref List<Exception> targetList, Exception exception)
        {
            AggregateException aggregateException = exception as AggregateException;

            if (targetList == null)
            {
                targetList = new List<Exception>();
            }

            if (aggregateException != null)
            {
                targetList.Add(aggregateException.InnerExceptions.Count == 1 ? exception.InnerException : exception);
            }
            else
            {
                targetList.Add(exception);
            }
        }
    }
}
