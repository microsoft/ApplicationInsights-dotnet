namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Represents method used to configure <see cref="ITelemetryModule"/> with dependency injection support.
    /// </summary>
    internal class TelemetryModuleConfigurator : ITelemetryModuleConfigurator
    {
        private readonly Action<ITelemetryModule> configure;
        internal readonly Type telemetryModuleType;

        /// <summary>
        /// Constructs an instance of <see cref="TelemetryModuleConfigurator"/>.
        /// </summary>        
        /// <param name="configure">The action used to configure telemetry module.</param>
        /// <param name="telemetryModuleType">The type of the telemetry module being configured.</param>
        public TelemetryModuleConfigurator(Action<ITelemetryModule> configure, Type telemetryModuleType)
        {
            this.configure = configure;
            this.TelemetryModuleType = telemetryModuleType;
        }

        /// <summary>
        /// Creates an instance of the telemetry processor, passing the
        /// next <see cref="ITelemetryProcessor"/> in the call chain to
        /// its constructor.
        /// </summary>
        public void Configure(ITelemetryModule telemetryModule)
        {
            this.configure?.Invoke(telemetryModule);
        }

        /// <summary>
        /// Gets the type of <see cref="ITelemetryModule"/> to be configured.     
        /// </summary>
        public Type TelemetryModuleType { get; }
    }
}