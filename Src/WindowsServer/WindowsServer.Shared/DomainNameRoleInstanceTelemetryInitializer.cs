namespace Microsoft.ApplicationInsights.WindowsServer
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Obsolete. A telemetry context initializer that used to populate role instance name. Preserved for backward compatibility.
    /// </summary>
    public class DomainNameRoleInstanceTelemetryInitializer : ITelemetryInitializer
    {   
        /// <summary>
        /// Initializes <see cref="ITelemetry" /> device context.
        /// </summary>
        /// <param name="telemetry">The telemetry to initialize.</param>
        public void Initialize(ITelemetry telemetry)
        {            
        }
    }
}
