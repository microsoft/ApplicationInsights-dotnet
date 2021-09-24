namespace Microsoft.AspNetCore.Hosting
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="IConfigureOptions&lt;ApplicationInsightsServiceOptions&gt;"/> implementation that reads options from 'appsettings.json',
    /// environment variables and sets developer mode based on debugger state.
    /// </summary>
    internal class DefaultApplicationInsightsServiceConfigureOptions : IConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IConfiguration userConfiguration;
        private readonly string environmentName;
        private readonly string contentRootPath;

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultApplicationInsightsServiceConfigureOptions"/> class.
        /// </summary>
        /// <param name="hostEnvironment"><see cref="IHostEnvironment"/> to use for retreiving ContentRootPath.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>  from an application.</param>
        public DefaultApplicationInsightsServiceConfigureOptions(IHostEnvironment hostEnvironment, IConfiguration configuration = null)
        {
            this.environmentName = hostEnvironment?.EnvironmentName;
            this.contentRootPath = hostEnvironment?.ContentRootPath ?? Directory.GetCurrentDirectory();

            this.userConfiguration = configuration;
        }

        [Obsolete("IHostingEnvironment is obsolete and will be removed in a future version. The recommended alternative is Microsoft.Extensions.Hosting.IHostEnvironment.", false)]
        public DefaultApplicationInsightsServiceConfigureOptions(IHostEnvironment hostEnvironment, IHostingEnvironment hostingEnvironment, IConfiguration configuration = null)
        {
            this.environmentName = hostEnvironment?.EnvironmentName ?? hostingEnvironment?.EnvironmentName;
            this.contentRootPath = hostEnvironment?.ContentRootPath ?? hostEnvironment?.ContentRootPath ?? Directory.GetCurrentDirectory();

            this.userConfiguration = configuration;
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultApplicationInsightsServiceConfigureOptions"/> class.
        /// </summary>
        /// <param name="hostingEnvironment"><see cref="IHostingEnvironment"/> to use for retreiving ContentRootPath.</param>
        /// <param name="configuration"><see cref="IConfiguration"/>  from an application.</param>
        [Obsolete("IHostingEnvironment is obsolete and will be removed in a future version. The recommended alternative is Microsoft.Extensions.Hosting.IHostEnvironment.", false)]
        public DefaultApplicationInsightsServiceConfigureOptions(IHostingEnvironment hostingEnvironment, IConfiguration configuration = null)
        {
            this.environmentName = hostingEnvironment?.EnvironmentName;
            this.contentRootPath = hostingEnvironment?.ContentRootPath ?? Directory.GetCurrentDirectory();

            this.userConfiguration = configuration;
        }

        /// <inheritdoc />
        public void Configure(ApplicationInsightsServiceOptions options)
        {
            var configBuilder = new ConfigurationBuilder().SetBasePath(this.contentRootPath);

            if (this.userConfiguration != null)
            {
                configBuilder.AddConfiguration(this.userConfiguration);
            }

            configBuilder.AddJsonFile("appsettings.json", true);

            if (this.environmentName != null)
            {
                configBuilder.AddJsonFile(string.Format(CultureInfo.InvariantCulture, "appsettings.{0}.json", this.environmentName), true);
            }

            configBuilder.AddEnvironmentVariables();

            ApplicationInsightsExtensions.AddTelemetryConfiguration(configBuilder.Build(), options);

            if (Debugger.IsAttached)
            {
                options.DeveloperMode = true;
            }
        }
    }
}
