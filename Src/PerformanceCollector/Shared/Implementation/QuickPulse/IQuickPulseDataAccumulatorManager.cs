namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    internal interface IQuickPulseDataAccumulatorManager
    {
        /// <summary>
        /// Gets a reference to the accumulator that is currently under construction.
        /// </summary>
        QuickPulseDataAccumulator CurrentDataAccumulator { get; }

        /// <summary>
        /// Locks in the current data accumulator and moves it into the Complete slot.
        /// Resets the Current slot to a new zeroed-out accumulator.
        /// </summary>
        /// <returns>The newly completed accumulator.</returns>
        QuickPulseDataAccumulator CompleteCurrentDataAccumulator();
    }
}