namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Threading;

    /// <summary>
    /// Accumulator manager for QuickPulse data.
    /// </summary>
    internal class QuickPulseDataAccumulatorManager : IQuickPulseDataAccumulatorManager
    {
        private QuickPulseDataAccumulator currentDataAccumulator = new QuickPulseDataAccumulator();
        private QuickPulseDataAccumulator completedDataAccumulator;

        public QuickPulseDataAccumulator CurrentDataAccumulatorReference
        {
            get { return this.currentDataAccumulator; }
        }

        public QuickPulseDataAccumulator CompletedDataAccumulator
        {
            get { return this.completedDataAccumulator; }
        }
        
        public QuickPulseDataAccumulator CompleteCurrentDataAccumulator()
        {
            /* 
                Here we need to 
                    - promote currentDataAccumulator to completedDataAccumulator
                    - reset (zero out) the new currentDataAccumulator

                We're not using critical sections, so THE RESULT WILL BE SLIGHTLY INCORRECT because the writers will keep writing throughout this operation
                causing data inconsistencies (e.g. 4 requests with 3 failures when in fact all 4 requests failed).
                In other words, some telemetry items will be partially counted towards the older accumulator, and partially - towards the newer one.
                See unit tests for details on this "spraying" behavior.
            */ 
            
            Interlocked.Exchange(ref this.completedDataAccumulator, this.currentDataAccumulator);

            Interlocked.Exchange(ref this.currentDataAccumulator, new QuickPulseDataAccumulator());

            var timestamp = DateTime.UtcNow;
            this.completedDataAccumulator.EndTimestamp = timestamp;
            this.currentDataAccumulator.StartTimestamp = timestamp;

            return this.completedDataAccumulator;
        }
    }
}