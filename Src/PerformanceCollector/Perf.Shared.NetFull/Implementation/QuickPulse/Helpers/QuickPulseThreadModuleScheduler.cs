namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse.Helpers
{
    using System;
    using System.Threading;

    internal class QuickPulseThreadModuleScheduler : IQuickPulseModuleScheduler
    {
        public static readonly QuickPulseThreadModuleScheduler Instance = new QuickPulseThreadModuleScheduler();

        private QuickPulseThreadModuleScheduler()
        {
        }

        public IQuickPulseModuleSchedulerHandle Execute(Action<CancellationToken> action)
        {
            State state = new State(action);

            return state;
        }

        private class State : IQuickPulseModuleSchedulerHandle
        {
            private readonly Thread thread;

            private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            public State(Action<CancellationToken> action)
            {
                this.thread = new Thread(this.Worker) { IsBackground = true };
                this.thread.Start(action);
            }

            public void Stop(bool wait)
            {
                this.Dispose();
                if (wait)
                {
                    this.thread.Join();
                }
            }

            public void Dispose()
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
            }

            private void Worker(object state)
            {
                (state as Action<CancellationToken>)?.Invoke(this.cancellationTokenSource.Token);
            }
        }
    }
}
