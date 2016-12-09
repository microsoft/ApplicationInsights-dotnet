namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;
   
    /// <summary>
    /// A telemetry initializer that will gather Azure Web App Role Environment context information.
    /// </summary>
    public class AzureWebAppRoleEnvironmentTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>Azure Web App name corresponding to the resource name.</summary>
        private const string WebAppNameEnvironmentVariable = "WEBSITE_SITE_NAME";

        /// <summary>Azure Web App Hostname. This will include the deployment slot, but will be same across instances of same slot.</summary>
        private const string WebAppHostNameEnvironmentVariable = "WEBSITE_HOSTNAME";

        private string roleInstanceName;
        private string roleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureWebAppRoleEnvironmentTelemetryInitializer" /> class.
        /// </summary>
        public AzureWebAppRoleEnvironmentTelemetryInitializer()
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
                string name = LazyInitializer.EnsureInitialized(ref this.roleName, this.GetRoleName);
                telemetry.Context.Cloud.RoleName = name;
            }

            if (string.IsNullOrEmpty(telemetry.Context.GetInternalContext().NodeName))
            {
                string name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetNodeName);
                telemetry.Context.GetInternalContext().NodeName = name;
            }
        }

        private string GetRoleName()
        {
            return Environment.GetEnvironmentVariable(WebAppNameEnvironmentVariable) ?? string.Empty;
        }

        private string GetNodeName()
        {
            return Environment.GetEnvironmentVariable(WebAppHostNameEnvironmentVariable) ?? string.Empty;
        }
    }
}
