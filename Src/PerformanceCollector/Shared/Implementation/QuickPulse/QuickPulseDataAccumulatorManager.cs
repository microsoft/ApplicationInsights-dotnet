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

        public QuickPulseDataAccumulator CurrentDataAccumulator
        {
            get { return this.currentDataAccumulator; }
        }
        
        public QuickPulseDataAccumulator CompleteCurrentDataAccumulator()
        {
            /* 
                Here we need to 
                    - promote currentDataAccumulator to completedDataAccumulator
                    - reset (zero out) the new currentDataAccumulator

                Even though there are two separate Exchange() calls below, no data loss will occure since completedDataAccuulator will not be read until later on this same thread,
                so the first Exchange doesn't really do anything just yet.
                However, certain telemetry items will be "sprayed" between two neighboring accumulators due to the fact that the snap might occure in the middle of a reader executing its Interlocked's.
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