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
        internal CloudContext()
        {
        }

        /// <summary>
        /// Gets or sets the role name.
        /// </summary>
        public string RoleName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        public string RoleInstance
        {
            get;
            set;
        }

        internal void UpdateTags(IDictionary<string, string> tags)
        {
            tags.UpdateTagValue(ContextTagKeys.Keys.CloudRole, this.RoleName);
            tags.UpdateTagValue(ContextTagKeys.Keys.CloudRoleInstance, this.RoleInstance);
        }

        internal void CopyTo(TelemetryContext telemetryContext)
        {
            var target = telemetryContext.Cloud;
            target.RoleName = Tags.CopyTagValue(target.RoleName, this.RoleName);
            target.RoleInstance = Tags.CopyTagValue(target.RoleInstance, this.RoleInstance);
        }
    }
}
