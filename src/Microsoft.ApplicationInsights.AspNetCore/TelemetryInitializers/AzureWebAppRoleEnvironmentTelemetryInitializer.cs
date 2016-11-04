namespace Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers
{
    using System;
    using System.Threading;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.AspNetCore.Http;
    using DataContracts;

    /// <summary>
    /// A telemetry initializer that will gather Azure Web App Role Environment context information.
    /// </summary>
    public class AzureWebAppRoleEnvironmentTelemetryInitializer : TelemetryInitializerBase
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

            if (string.IsNullOrEmpty(telemetry.Context.GetInternalContext().NodeName))
            {
                string name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, this.GetRoleInstanceName);
                telemetry.Context.GetInternalContext().NodeName = name;
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
