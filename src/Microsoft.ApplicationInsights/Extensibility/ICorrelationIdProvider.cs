namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>
    /// An interface for getting a correlation id from a provided instrumentation key
    /// </summary>
    public interface ICorrelationIdProvider
    {
        /// <summary>
        /// Retrieves the correlation id to be used for Request.Source or Dependency.Target
        /// </summary>
        /// <remarks>
        /// Application Insights data model defines two fields: request.source and dependency.target. 
        /// The first field identifies the component that initiated the dependency request, 
        /// and the second identifies which component returned the response of the dependency call.
        /// For more information see: https://docs.microsoft.com/azure/application-insights/application-insights-correlation
        /// </remarks>
        /// <param name="instrumentationKey">Instrumentation Key string used to lookup associated Correlation Id.</param>
        /// <param name="correlationId">Correlation Id corresponding to the Instrumentation Key</param>
        /// <returns>TRUE if Correlation Id was successfully retrieved; FALSE otherwise.</returns>
        bool TryGetCorrelationId(string instrumentationKey, out string correlationId);
    }
}
