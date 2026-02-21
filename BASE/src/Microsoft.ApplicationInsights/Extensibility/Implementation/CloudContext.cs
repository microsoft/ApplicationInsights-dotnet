namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    /// <summary>
    /// Encapsulates information about a cloud where an application is running.
    /// </summary>
    internal sealed class CloudContext
    {
        /// <summary>
        /// Environment variable key used to communicate cloud role name override to the exporter.
        /// </summary>
        internal const string CloudRoleNameEnvironmentVariable = "MICROSOFT_APPLICATIONINSIGHTS_CLOUD_ROLE_NAME";

        /// <summary>
        /// Environment variable key used to communicate cloud role instance override to the exporter.
        /// </summary>
        internal const string CloudRoleInstanceEnvironmentVariable = "MICROSOFT_APPLICATIONINSIGHTS_CLOUD_ROLE_INSTANCE";

        private string roleName;
        private string roleInstance;

        internal CloudContext()
        {
        }

        /// <summary>
        /// Gets or sets the role name.
        /// </summary>
        public string RoleName
        {
            get
            {
                return string.IsNullOrEmpty(this.roleName) ? null : this.roleName;
            }

            set
            {
                this.roleName = value;
                Environment.SetEnvironmentVariable(CloudRoleNameEnvironmentVariable, value);
            }
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        public string RoleInstance
        {
            get
            {
                return string.IsNullOrEmpty(this.roleInstance) ? null : this.roleInstance;
            }

            set
            {
                this.roleInstance = value;
                Environment.SetEnvironmentVariable(CloudRoleInstanceEnvironmentVariable, value);
            }
        }
    }
}
