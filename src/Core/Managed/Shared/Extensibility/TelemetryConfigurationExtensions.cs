namespace Microsoft.ApplicationInsights.Extensibility
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Extension methods for TelemetryConfiguration.
    /// </summary>
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>
        /// Returns the TelemetryProcessorChainBuilder which can build and populate TelemetryProcessors in the given config.
        /// </summary>
        /// <param name="configuration">Telemetry configuration for which the telemetry processor chain is to be setup.</param>        
        /// <returns>The constructed TelemetryProcessorChainBuilder.</returns>
        public static TelemetryProcessorChainBuilder GetTelemetryProcessorChainBuilder(this TelemetryConfiguration configuration)
        {
            return new TelemetryProcessorChainBuilder(configuration);
        }
    }
}
