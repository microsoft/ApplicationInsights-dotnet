namespace Microsoft.ApplicationInsights.WorkerService
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="IConfigureOptions&lt;ApplicationInsightsServiceOptions&gt;"/> implementation that reads options from provided IConfiguration    
    /// </summary>
    internal class DefaultApplicationInsightsServiceConfigureOptions : IConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultApplicationInsightsServiceConfigureOptions"/>
        /// </summary>
        /// <param name="configuration"><see cref="IConfiguration"/> from which configuraion for ApplicationInsights can be retrieved.</param>
        public DefaultApplicationInsightsServiceConfigureOptions(IConfiguration configuration = null)
        {
            this.configuration = configuration;
        }

        /// <inheritdoc />
        public void Configure(ApplicationInsightsServiceOptions options)
        {
            if (configuration != null)
            {
                ApplicationInsightsExtensions.AddTelemetryConfiguration(configuration, options);
            }

            if (Debugger.IsAttached)
            {
                options.DeveloperMode = true;
            }
        }
    }
}
