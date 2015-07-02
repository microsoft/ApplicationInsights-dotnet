namespace System.Runtime.CompilerServices
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;

    [StructLayout(LayoutKind.Sequential), HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
    internal struct AsyncTaskMethodBuilder<TResult> : IAsyncMethodBuilder
    {
        internal static readonly Task<TResult> s_defaultResultTask = AsyncTaskCache.CreateCacheableTask<TResult>(default(TResult));

        private TaskCompletionSource<TResult> taskCompletionSource;

        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            AsyncTaskMethodBuilder<TResult> builder = new AsyncTaskMethodBuilder<TResult>() { taskCompletionSource = new TaskCompletionSource<TResult>() };
            return builder;
        }

        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (((TStateMachine)stateMachine) == null)
            {
                throw new ArgumentNullException("stateMachine");
            }

            ////Thread currentThread = Thread.CurrentThread;
            ////ExecutionContextSwitcher ecsw = new ExecutionContextSwitcher();
            ////RuntimeHelpers.PrepareConstrainedRegions();
            ////try
            ////{
            ////    ExecutionContext.EstablishCopyOnWriteScope(currentThread, false, ref ecsw);
            stateMachine.MoveNext();
            ////}
            ////finally
            ////{
            ////    ecsw.Undo(currentThread);
            ////}
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            // Note: too complicated to pull in and we won't be using it anyway
            throw new InvalidOperationException();
        }

        void IAsyncMethodBuilder.PreBoxInitialization<TStateMachine>(ref TStateMachine stateMachine)
        {
            ////Task<TResult> task = this.Task;
            ////if (AsyncCausalityTracer.LoggingOn)
            ////{
            ////    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, task.Id, "Async: " + stateMachine.GetType().Name, 0L);
            ////}
            ////if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
            ////{
            ////    System.Threading.Tasks.Task.AddToActiveTasks(task);
            ////}
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // Note: this boils down to chaining the on completed event of the awaiter to the move next for the state machine
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            // Note: same as above
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public Task<TResult> Task
        {
            get
            {
                return this.taskCompletionSource.Task;
            }
        }

        public void SetResult(TResult result)
        {
            this.taskCompletionSource.SetResult(result);
        }

        internal void SetResult(Task<TResult> completedTask)
        {
            this.taskCompletionSource.SetResult(completedTask.Result);
        }

        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            OperationCanceledException cancellationException = exception as OperationCanceledException;
            if (!((cancellationException != null) ? this.taskCompletionSource.TrySetCanceled() : this.taskCompletionSource.TrySetException(exception)))
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            ////this.Task.SetNotificationForWaitCompletion(enabled);
        }

        private object ObjectIdForDebugger
        {
            get
            {
                return this.Task;
            }
        }

        object IAsyncMethodBuilder.ObjectIdForDebugger
        {
            get
            {
                return this.ObjectIdForDebugger;
            }
        }
    }
}
