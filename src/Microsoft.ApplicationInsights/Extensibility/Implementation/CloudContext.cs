namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates information about a cloud where an application is running.
    /// </summary>
    public sealed class CloudContext
    {
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
            get { return this.roleName == string.Empty ? null : this.roleName; }
            set { this.roleName = value; }
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        public string RoleInstance
        {
            get { return this.roleInstance == string.Empty ? null : this.roleInstance; }
            set { this.roleInstance = value; }
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.CloudRole, this.RoleName);
            tags.UpdateTagValue(ContextTagKeys.Keys.CloudRoleInstance, this.RoleInstance);
        }

        internal void CopyFrom(TelemetryContext telemetryContext)
        {
            var source = telemetryContext.Cloud;
            Tags.CopyTagValue(source.RoleName, ref this.roleName);
            Tags.CopyTagValue(source.RoleInstance, ref this.roleInstance);
        }
    }
}
