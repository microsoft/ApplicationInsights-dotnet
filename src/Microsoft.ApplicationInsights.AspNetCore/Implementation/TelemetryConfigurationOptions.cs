namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The <see cref="IOptions{TelemetryConfiguration}"/> implementation that create new <see cref="TelemetryConfiguration"/> every time when called"/>.
    /// </summary>
    internal class TelemetryConfigurationOptions : IOptions<TelemetryConfiguration>
    {
        public TelemetryConfigurationOptions(IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions)
        {
            // workaround for Microsoft/ApplicationInsights-dotnet#613
            this.Value = TelemetryConfiguration.CreateDefault();

            var configureOptionsArray = configureOptions.ToArray();
            foreach (var c in configureOptionsArray)
            {
                c.Configure(this.Value);
            }

            // as we expect some customers to use TelemetryConfiguration.Active even together with DependencyInjection
            // we make sure it has been set up
            // it must be done only once even if there are multiple Web Hosts in the process
            if (!IsActiveConfigured(this.Value.InstrumentationKey))
            { 
                foreach (var c in configureOptionsArray)
                {
                    c.Configure(TelemetryConfiguration.Active);
                }
            }
        }

        /// <inheritdoc />
        public TelemetryConfiguration Value { get; }

        /// <summary>
        /// Determines is TelemetryConfiguration.Active needs to be configured
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key configuration</param>
        /// <returns>True is Active TelemertryConfiguration was previously configured</returns>
        private bool IsActiveConfigured(string instrumentationKey)
        {
            var active = TelemetryConfiguration.Active;
            if (string.IsNullOrEmpty(active.InstrumentationKey) && !string.IsNullOrEmpty(instrumentationKey))
            {
                return false;
            }

            if (active.TelemetryInitializers.Count <= 1 && active.TelemetryProcessors.Count <= 1)
            {
                return false;
            }

            return true;
        }
    }
}