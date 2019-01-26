#if DEPENDENCY_COLLECTOR
namespace Microsoft.ApplicationInsights.W3C
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.ApplicationInsights.Channel;    
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Telemetry Initializer that sets correlation ids for W3C.
    /// </summary>
    [Obsolete("Use Microsoft.ApplicationInsights.Extensibility.W3C.W3COperationCorrelationTelemetryInitializer in Microsoft.ApplicationInsights package instead.")]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification =
        "TelemetryInitializers are intended to be instantiated by the framework when added to a config.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class W3COperationCorrelationTelemetryInitializer : ITelemetryInitializer
    {
        private readonly Extensibility.W3C.W3COperationCorrelationTelemetryInitializer internalInitializer = new Extensibility.W3C.W3COperationCorrelationTelemetryInitializer();

        /// <summary>
        /// Initializes telemetry item.
        /// </summary>
        /// <param name="telemetry">Telemetry item.</param>
        public void Initialize(ITelemetry telemetry)
        {
            this.internalInitializer.Initialize(telemetry);
        }
    }
}
#endif