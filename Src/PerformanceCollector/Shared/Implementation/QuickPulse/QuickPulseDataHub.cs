namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System.Threading;

    /// <summary>
    /// Data hub for QuickPulse data.
    /// </summary>
    internal sealed class QuickPulseDataHub : IQuickPulseDataHub
    {
        private static readonly object syncRoot = new object();

        private static volatile QuickPulseDataHub instance;

        private QuickPulseDataSample currentDataSample = new QuickPulseDataSample();
        private QuickPulseDataSample completedDataSample;

        private QuickPulseDataHub()
        {
        }

        public static QuickPulseDataHub Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new QuickPulseDataHub();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Unit tests only.
        /// </summary>
        internal static void ResetInstance()
        {
            lock (syncRoot)
            {
                instance = new QuickPulseDataHub();
            }
        }

        public QuickPulseDataSample CurrentDataSampleReference => this.currentDataSample;

        public QuickPulseDataSample CompletedDataSample => this.completedDataSample;

        public QuickPulseDataSample CompleteCurrentDataSample()
        {
            /* 
                Here we need to 
                    - promote currentDataSample to completedDataSample
                    - reset (zero out) the new currentDataSample

                We're not using critical sections, so THE RESULT WILL BE SLIGHTLY INCORRECT because the writers will keep writing throughout this operation
                causing data inconsistencies (e.g. 4 requests with 3 failures when in fact all 4 requests failed).
                In other words, some telemetry items will be partially counted towards the older sample, and partially - towards the newer one.
                See unit tests for details on this "spraying" behavior.
            */ 
            
            Interlocked.Exchange(ref this.completedDataSample, this.currentDataSample);

            Interlocked.Exchange(ref this.currentDataSample, new QuickPulseDataSample());

            return this.completedDataSample;
        }
    }
}