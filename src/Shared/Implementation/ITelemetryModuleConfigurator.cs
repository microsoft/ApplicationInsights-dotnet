#if AI_ASPNETCORE_WEB
    namespace Microsoft.ApplicationInsights.AspNetCore
#else
    namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
#if AI_ASPNETCORE_WEB
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
#else
    using Microsoft.ApplicationInsights.WorkerService;
#endif
    using Microsoft.ApplicationInsights.Extensibility;
    using System;

    /// <summary>
    /// Represents method used to configure <see cref="ITelemetryModule"/> with dependency injection support.
    /// </summary>
    public interface ITelemetryModuleConfigurator
    {
        /// <summary>
        /// Gets the type of <see cref="ITelemetryModule"/> to be configured.
        /// </summary>
        Type TelemetryModuleType { get; }

        /// <summary>
        /// Configures the given <see cref="ITelemetryModule"/>
        /// </summary>
        [Obsolete("Use Configure(ITelemetryModule telemetryModule, ApplicationInsightsServiceOptions options) instead.")]
        void Configure(ITelemetryModule telemetryModule);

        /// <summary>
        /// Configures the given <see cref="ITelemetryModule"/>.
        /// </summary>
        void Configure(ITelemetryModule telemetryModule, ApplicationInsightsServiceOptions options);
    }
}