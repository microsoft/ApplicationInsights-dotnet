namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A generic factory for telemetry processors of a given type.
    /// </summary>
    /// <typeparam name="T">The type of telemetry processor created by this factory.</typeparam>
    internal class TelemetryProcessorFactory<T> : ITelemetryProcessorFactory where T : ITelemetryProcessor
    {
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Constructs an instance of the factory.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public TelemetryProcessorFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates an instance of the telemetry processor, passing the
        /// next <see cref="ITelemetryProcessor"/> in the call chain to
        /// its constructor.
        /// </summary>
        public ITelemetryProcessor Create(ITelemetryProcessor next)
        {
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, next);
        }
    }
}
