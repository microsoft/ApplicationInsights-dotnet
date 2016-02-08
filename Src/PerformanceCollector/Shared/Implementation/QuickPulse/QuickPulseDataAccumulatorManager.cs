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
        
        public QuickPulseDataAccumulator CompleteCurrentDataAccumulator()
        {
            /* 
                Here we need to 
                    - promote currentDataAccumulator to completedDataAccumulator
                    - reset (zero out) the new currentDataAccumulator

                There's a low chance of data loss here (if a writer accesses the accumulator between the two Exchange calls below), but 
                we should be ok with that given the overall number of items.
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