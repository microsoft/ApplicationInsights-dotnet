#if AI_ASPNETCORE_WEB
namespace Microsoft.AspNetCore.Hosting
#else
namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using System.Diagnostics.CodeAnalysis;
#if AI_ASPNETCORE_WEB
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
#endif
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="IConfigureOptions{ApplicationInsightsServiceOptions}"/> implementation that reads options from provided IConfiguration.
    /// </summary>
    internal class DefaultApplicationInsightsServiceConfigureOptions : IConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultApplicationInsightsServiceConfigureOptions"/> class.
        /// </summary>
        /// <param name="configuration"><see cref="IConfiguration"/> from which configuration for ApplicationInsights can be retrieved.</param>
        public DefaultApplicationInsightsServiceConfigureOptions(IConfiguration configuration = null)
        {
            this.configuration = configuration;
        }

        /// <inheritdoc />
        public void Configure(ApplicationInsightsServiceOptions options)
        {
            if (this.configuration != null)
            {
                ApplicationInsightsExtensions.AddTelemetryConfiguration(this.configuration, options);
            }
        }
    }
}
