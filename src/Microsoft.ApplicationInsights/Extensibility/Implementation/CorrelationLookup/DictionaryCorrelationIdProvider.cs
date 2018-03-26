namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System.Collections.Generic;

    /// <summary>
    /// Correlation Provider that holds a dictionary of ikeys to correlationIds.
    /// </summary>
    public class DictionaryCorrelationIdProvider : ICorrelationIdProvider
    {
        /// <summary>
        /// Gets or sets a dictionary of Ikeys to CorrelationIds
        /// </summary>
        public Dictionary<string, string> DefinedIds { get; set; }

        /// <summary>
        /// Provides a Correlation Id based on an ikey.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation Key string used to lookup associated Correlation Id.</param>
        /// <param name="correlationId">Correlation Id corresponding to the Instrumentation Key</param>
        /// <returns>TRUE if Correlation Id was successfully retrieved; FALSE otherwise.</returns>
        public bool TryGetCorrelationId(string instrumentationKey, out string correlationId)
        {
            if (this.DefinedIds == null)
            {
                correlationId = null;
                return false;
            }

            return this.DefinedIds.TryGetValue(instrumentationKey, out correlationId);
        }
    }
}
