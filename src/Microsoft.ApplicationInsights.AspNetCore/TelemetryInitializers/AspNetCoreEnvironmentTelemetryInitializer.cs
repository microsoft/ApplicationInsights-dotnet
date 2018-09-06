namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// <see cref="ITelemetryInitializer"/> implementation that stamps ASP.NET Core environment name
    /// on telemetries.
    /// </summary>
    public class AspNetCoreEnvironmentTelemetryInitializer: ITelemetryInitializer
    {
        private const string AspNetCoreEnvironmentPropertyName = "AspNetCoreEnvironment";
        private readonly IHostingEnvironment environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreEnvironmentTelemetryInitializer"/> class.
        /// </summary>
        public AspNetCoreEnvironmentTelemetryInitializer(IHostingEnvironment environment)
        {
            this.environment = environment;
        }

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (environment != null && !telemetry.Context.GlobalProperties.ContainsKey(AspNetCoreEnvironmentPropertyName))
            {
                telemetry.Context.GlobalProperties.Add(AspNetCoreEnvironmentPropertyName, environment.EnvironmentName);
            }
        }
    }
}
