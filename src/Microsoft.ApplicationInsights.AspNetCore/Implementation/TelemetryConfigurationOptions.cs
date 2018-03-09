namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The <see cref="IOptions{TelemetryConfiguration}"/> implementation that uses <see cref="TelemetryConfiguration.Active"/> as initial value.
    /// </summary>
    internal class TelemetryConfigurationOptions : IOptions<TelemetryConfiguration>
    {
        public TelemetryConfigurationOptions(IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions)
        {
            // workaround for Microsoft/ApplicationInsights-dotnet#613
            this.Value = new TelemetryConfiguration();
            this.Value.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            foreach (var c in configureOptions)
            {
                c.Configure(this.Value);
                c.Configure(TelemetryConfiguration.Active);
            }
        }

        /// <inheritdoc />
        public TelemetryConfiguration Value { get; }
    }
}