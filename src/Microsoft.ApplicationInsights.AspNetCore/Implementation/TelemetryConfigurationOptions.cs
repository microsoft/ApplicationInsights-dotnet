namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;

    internal class TelemetryConfigurationOptions : IOptions<TelemetryConfiguration>
    {
        public TelemetryConfigurationOptions(IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions)
        {
            foreach (var c in configureOptions)
            {
                c.Configure(this.Value);
            }
        }

        public TelemetryConfiguration Value => TelemetryConfiguration.Active;
    }
}