namespace System.Threading.Tasks
{
    using System.Runtime.CompilerServices;

    internal struct ConfiguredTaskAwaiter : INotifyCompletion
    {
        private readonly Task task;

        private readonly bool continueOnCapturedContext;

        internal ConfiguredTaskAwaiter(Task task, bool continueOnCapturedContext)
        {
            this.task = task;
            this.continueOnCapturedContext = continueOnCapturedContext;
        }

        public bool IsCompleted
        {
            get { return this.task.IsCompleted; }
        }

        public void OnCompleted(Action continuation)
        {
            this.task.ContinueWith(_ => continuation(), this.continueOnCapturedContext == true ? TaskEx.CapturedTaskScheduler : TaskEx.DefaultTaskScheduler);
        }

        public void GetResult()
        {
            try
            {
                this.task.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions[0];
            }
        }
    }
}
