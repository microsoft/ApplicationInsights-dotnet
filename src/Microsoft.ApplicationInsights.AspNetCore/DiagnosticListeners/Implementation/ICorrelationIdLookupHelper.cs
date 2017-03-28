namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    /// <summary>
    /// An interface for getting a correlation id from a provided instrumentation key.
    /// </summary>
    internal interface ICorrelationIdLookupHelper
    {
        /// <summary>
        /// Retrieves the correlation id corresponding to a given instrumentation key.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key string.</param>
        /// <param name="correlationId">AppId corresponding to the provided instrumentation key.</param>
        /// <returns>true if correlationId was successfully retrieved; false otherwise.</returns>
        bool TryGetXComponentCorrelationId(string instrumentationKey, out string correlationId);
    }
}
