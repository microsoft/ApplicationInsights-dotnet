namespace Microsoft.ApplicationInsights
{
    using System.ComponentModel;
    using Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Extension methods for TelemetryConfiguration class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>
        /// Adds logic to track operations correlation.
        /// </summary>
        /// <param name="configuraton">Telemetry configuration object to add track operation correlation for.</param>
        public static void AddOperationsApi(this TelemetryConfiguration configuraton)
        {
            configuraton.TelemetryInitializers.Insert(0, new CallContextBasedOperationCorrelationTelemetryInitializer());
        }
    }
}
