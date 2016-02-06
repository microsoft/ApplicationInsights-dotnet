namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    internal interface IQuickPulseServiceClient
    {
        /// <summary>
        /// Pings QPS to check if it expects data right now.
        /// </summary>
        /// <returns><b>true</b> if data is expected, otherwise <b>false</b>.</returns>
        bool Ping();

        /// <summary>
        /// Submits a data sample to QPS.
        /// </summary>
        /// <param name="sample">Data sample.</param>
        /// <returns><b>true</b> if the client is expected to keep sending data samples, <b>false</b> otherwise.</returns>
        bool SubmitSample(QuickPulseDataSample sample);
    }
}