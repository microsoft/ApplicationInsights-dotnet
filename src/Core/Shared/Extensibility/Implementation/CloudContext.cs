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
        private readonly IDictionary<string, string> tags;

        internal CloudContext(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }
        
        /// <summary>
        /// Gets or sets the role name.
        /// </summary>
        public string RoleName
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.CloudRole); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.CloudRole, value); }
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        public string RoleInstance
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.CloudRoleInstance); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.CloudRoleInstance, value); }
        }
    }
}
