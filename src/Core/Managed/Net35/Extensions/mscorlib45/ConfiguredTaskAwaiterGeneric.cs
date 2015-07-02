namespace System.Threading.Tasks
{
    using System.Runtime.CompilerServices;

    internal struct ConfiguredTaskAwaiter<TResult> : INotifyCompletion
    {
        private readonly Task<TResult> task;

        private readonly bool continueOnCapturedContext;

        internal ConfiguredTaskAwaiter(Task<TResult> task, bool continueOnCapturedContext)
        {
            this.task = task;
            this.continueOnCapturedContext = continueOnCapturedContext;
        }

        public bool IsCompleted
        {
            get { return task.IsCompleted; }
        }

        public void OnCompleted(Action continuation)
        {
            this.task.ContinueWith(_ => continuation(), this.continueOnCapturedContext == true ? TaskEx.CapturedTaskScheduler : TaskEx.DefaultTaskScheduler);
        }

        public TResult GetResult()
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions[0];
            }
        }
    }
}
