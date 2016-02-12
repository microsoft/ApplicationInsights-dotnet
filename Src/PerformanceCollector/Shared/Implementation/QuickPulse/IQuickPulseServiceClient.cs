namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System.Collections.Generic;

    internal interface IQuickPulseServiceClient
    {
        /// <summary>
        /// Pings QPS to check if it expects data right now.
        /// </summary>
        /// <param name="instrumentationKey">InstrumentationKey for which to submit data samples.</param>
        /// <returns><b>true</b> if data is expected, otherwise <b>false</b>.</returns>
        bool? Ping(string instrumentationKey);

        /// <summary>
        /// Submits a data samples to QPS.
        /// </summary>
        /// <param name="samples">Data samples.</param>
        /// <param name="instrumentationKey">InstrumentationKey for which to submit data samples.</param>
        /// <returns><b>true</b> if the client is expected to keep sending data samples, <b>false</b> otherwise.</returns>
        bool? SubmitSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey);
    }
}