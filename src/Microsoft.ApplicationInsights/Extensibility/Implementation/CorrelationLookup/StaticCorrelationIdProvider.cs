namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    /// <summary>
    /// Correlation Id Provider that holds a single Correlation Id value that will be returned for every request.
    /// </summary>
    public class StaticCorrelationIdProvider : ICorrelationIdProvider
    {
        /// <summary>
        /// Gets or sets a Correlation Id.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Provides a single correlationId for every request.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation Key string used to lookup associated Correlation Id.</param>
        /// <param name="correlationId">Correlation Id corresponding to the Instrumentation Key</param>
        /// <returns>TRUE if Correlation Id was successfully retrieved; FALSE otherwise.</returns>
        public bool TryGetCorrelationId(string instrumentationKey, out string correlationId)
        {
            correlationId = this.CorrelationId;
            return !string.IsNullOrEmpty(this.CorrelationId);
        }
    }
}
