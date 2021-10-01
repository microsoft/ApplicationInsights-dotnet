namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// <see cref="ITelemetryInitializer"/> implementation that stamps ASP.NET Core environment name
    /// on telemetries.
    /// </summary>
    public class AspNetCoreEnvironmentTelemetryInitializer : ITelemetryInitializer
    {
        private const string AspNetCoreEnvironmentPropertyName = "AspNetCoreEnvironment";
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly IHostingEnvironment environment;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreEnvironmentTelemetryInitializer"/> class.
        /// </summary>
        /// <param name="environment">HostingEnvironment to provide EnvironmentName to be added to telemetry properties.</param>
#pragma warning disable CS0618 // Type or member is obsolete
        public AspNetCoreEnvironmentTelemetryInitializer(IHostingEnvironment environment)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            this.environment = environment;
        }

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (this.environment != null)
            {
                if (telemetry is ISupportProperties telProperties && !telProperties.Properties.ContainsKey(AspNetCoreEnvironmentPropertyName))
                {
                    telProperties.Properties.Add(
                        AspNetCoreEnvironmentPropertyName,
                        this.environment.EnvironmentName);
                }
            }
        }
    }
}
