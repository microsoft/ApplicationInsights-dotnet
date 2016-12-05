namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// A telemetry initializer that will gather Azure Web App Role Environment context information.
    /// </summary>
    internal class AzureWebAppRoleEnvironmentTelemetryInitializer : TelemetryInitializerBase
    {
        /// <summary>Azure Web App name corresponding to the resource name.</summary>
        private const string WebAppNameEnvironmentVariable = "WEBSITE_SITE_NAME";

        /// <summary>Azure Web App host that contains site name and slot: site-slot.</summary>
        private const string WebAppHostNameEnvironmentVariable = "WEBSITE_HOSTNAME";

        private string roleInstanceName;
        private string roleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureWebAppRoleEnvironmentTelemetryInitializer" /> class.
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        public AzureWebAppRoleEnvironmentTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// Initializes role name, role instance name and node name for Azure Web App case.
        /// </summary>
        /// <param name="platformContext">Platform context.</param>
        /// <param name="requestTelemetry">Request telemetry.</param>
        /// <param name="telemetry">Telemetry item.</param>
        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                string name = LazyInitializer.EnsureInitialized(ref this.roleName, this.GetRoleName);
                telemetry.Context.Cloud.RoleName = name;
            }

            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
            {
                string name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetRoleInstanceName);
                telemetry.Context.Cloud.RoleInstance = name;
            }

            InternalContext internalContext = telemetry.Context.GetInternalContext();
            if (string.IsNullOrEmpty(internalContext.NodeName))
            {
                string name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetRoleInstanceName);
                internalContext.NodeName = name;
            }
        }

        private string GetRoleName()
        {
            return Environment.GetEnvironmentVariable(WebAppNameEnvironmentVariable) ?? string.Empty;
        }

        private string GetRoleInstanceName()
        {
            return Environment.GetEnvironmentVariable(WebAppHostNameEnvironmentVariable) ?? string.Empty;
        }
    }
}
