namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
    
    /// <summary>
    /// A telemetry initializer that will gather Azure Role Environment context information.
    /// </summary>
    public class AzureRoleEnvironmentTelemetryInitializer : ITelemetryInitializer
    {
        private string roleInstanceName;
        private string roleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureRoleEnvironmentTelemetryInitializer" /> class.
        /// </summary>
        public AzureRoleEnvironmentTelemetryInitializer()
        {
            WindowsServerEventSource.Log.TelemetryInitializerLoaded(this.GetType().FullName);
        }

        /// <summary>
        /// Initializes <see cref="ITelemetry" /> device context.
        /// </summary>
        /// <param name="telemetry">The telemetry to initialize.</param>
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleName, AzureRoleEnvironmentContextReader.Instance.GetRoleName);
                telemetry.Context.Cloud.RoleName = name;
            }

            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, AzureRoleEnvironmentContextReader.Instance.GetRoleInstanceName);
                telemetry.Context.Cloud.RoleInstance = name;
            }
        }
    }
}
