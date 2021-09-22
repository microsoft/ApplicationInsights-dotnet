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
        /// <remarks>
        /// We don't cache the result of EnvironmentName because the environment can be changed while the app is running.
        /// </remarks>
        /// <param name="environment">HostingEnvironment to provide EnvironmentName to be added to telemetry properties.</param>
        [Obsolete("IHostingEnvironment is obsolete. The recommended alternative is Microsoft.Extensions.Hosting.IHostEnvironment.", false)]
        public AspNetCoreEnvironmentTelemetryInitializer(Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            this.getEnvironmentName = () => environment?.EnvironmentName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreEnvironmentTelemetryInitializer"/> class.
        /// </summary>
        /// <remarks>
        /// We don't cache the result of EnvironmentName because the environment can be changed while the app is running.
        /// </remarks>
        /// <param name="hostEnvironment">HostingEnvironment to provide EnvironmentName to be added to telemetry properties.</param>
        public AspNetCoreEnvironmentTelemetryInitializer(IHostEnvironment hostEnvironment)
        {
            this.getEnvironmentName = () => hostEnvironment?.EnvironmentName;
        }

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
