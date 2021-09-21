namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// <see cref="ITelemetryInitializer"/> implementation that stamps ASP.NET Core environment name
    /// on telemetries.
    /// </summary>
    public class AspNetCoreEnvironmentTelemetryInitializer : ITelemetryInitializer
    {
        private const string AspNetCoreEnvironmentPropertyName = "AspNetCoreEnvironment";
        private readonly Func<string> getEnvironmentName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreEnvironmentTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">HostingEnvironment to provide EnvironmentName to be added to telemetry properties.</param>
        [Obsolete("IHostingEnvironment is obsolete and will be removed in a future version. The recommended alternative is Microsoft.Extensions.Hosting.IHostEnvironment.", false)]
        public AspNetCoreEnvironmentTelemetryInitializer(IHostingEnvironment hostingEnvironment)
        {
            this.getEnvironmentName = () => hostingEnvironment?.EnvironmentName;
        }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreEnvironmentTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="hostEnvironment">HostingEnvironment to provide EnvironmentName to be added to telemetry properties.</param>
        public AspNetCoreEnvironmentTelemetryInitializer(IHostEnvironment hostEnvironment)
        {
            this.getEnvironmentName = () => hostEnvironment?.EnvironmentName;
        }
#endif

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (this.getEnvironmentName != null)
            {
                var environmentName = this.getEnvironmentName();
                if (environmentName != null)
                {
                    if (telemetry is ISupportProperties telProperties && !telProperties.Properties.ContainsKey(AspNetCoreEnvironmentPropertyName))
                    {
                        telProperties.Properties.Add(AspNetCoreEnvironmentPropertyName, environmentName);
                    }
                }
            }
        }
    }
}
