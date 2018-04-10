namespace Microsoft.ApplicationInsights.AspNetCore
{    
    using Microsoft.ApplicationInsights.Extensibility;
    using System;

    /// <summary>
    /// Represents method used to configure <see cref="ITelemetryModule"/> with dependency injection support.
    /// </summary>
    public interface ITelemetryModuleConfigurator
    {
        /// <summary>
        /// Configures the given <see cref="ITelemetryModule"/>     
        /// </summary>
        void Configure(ITelemetryModule telemetryModule);

        /// <summary>
        /// Gets the type of <see cref="ITelemetryModule"/> to be configured.     
        /// </summary>
        Type TelemetryModuleType { get; }
    }
}