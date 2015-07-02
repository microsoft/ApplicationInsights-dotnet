namespace System.Runtime.CompilerServices
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Threading.Tasks;

    [StructLayout(LayoutKind.Sequential), HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
    internal struct AsyncTaskMethodBuilder : IAsyncMethodBuilder
    {
        private static readonly Task<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;

        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder;

        public static AsyncTaskMethodBuilder Create()
        {
            AsyncTaskMethodBuilder builder = new AsyncTaskMethodBuilder { m_builder = AsyncTaskMethodBuilder<VoidTaskResult>.Create() };
            return builder;
        }

        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            this.m_builder.Start<TStateMachine>(ref stateMachine);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            this.m_builder.SetStateMachine(stateMachine);
        }

        void IAsyncMethodBuilder.PreBoxInitialization<TStateMachine>(ref TStateMachine stateMachine)
        {
            ////System.Threading.Tasks.Task task = this.Task;
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
            this.m_builder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            this.m_builder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        public System.Threading.Tasks.Task Task
        {
            get
            {
                return this.m_builder.Task;
            }
        }

        public void SetResult()
        {
            this.m_builder.SetResult(s_cachedCompleted);
        }

        public void SetException(Exception exception)
        {
            this.m_builder.SetException(exception);
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            this.m_builder.SetNotificationForWaitCompletion(enabled);
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

