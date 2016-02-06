namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    internal interface IQuickPulseDataHub
    {
        /// <summary>
        /// Gets a reference to the sample that is currently under construction.
        /// </summary>
        QuickPulseDataSample CurrentDataSampleReference { get; }

        /// <summary>
        /// Gets a reference to the sample that has been fully constructed and is now ready to go out. 
        /// </summary>
        QuickPulseDataSample CompletedDataSample { get; }

        /// <summary>
        /// Locks in the current data sample and moves it into the Complete slot.
        /// Resets the Current slot to a new zeroed-out sample.
        /// </summary>
        /// <returns>The newly completed sample.</returns>
        QuickPulseDataSample CompleteCurrentDataSample();
    }
}