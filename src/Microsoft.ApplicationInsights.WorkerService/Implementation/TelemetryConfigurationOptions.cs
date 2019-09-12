namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The <see cref="IOptions{TelemetryConfiguration}"/> implementation that create new <see cref="TelemetryConfiguration"/> every time when called".
    /// </summary>
    internal class TelemetryConfigurationOptions : IOptions<TelemetryConfiguration>
    {
        private static readonly object lockObject = new object();

        public TelemetryConfigurationOptions(IEnumerable<IConfigureOptions<TelemetryConfiguration>> configureOptions)
        {
            this.Value = TelemetryConfiguration.CreateDefault();

            var configureOptionsArray = configureOptions.ToArray();
            foreach (var c in configureOptionsArray)
            {
                c.Configure(this.Value);
            }
        }

        /// <inheritdoc />
        public TelemetryConfiguration Value { get; }
    }
}