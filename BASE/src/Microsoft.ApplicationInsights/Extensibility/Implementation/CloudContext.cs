namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    /// <summary>
    /// Encapsulates information about a cloud where an application is running.
    /// </summary>
    internal sealed class CloudContext
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
            get { return string.IsNullOrEmpty(this.roleName) ? null : this.roleName; }
            set { this.roleName = value; }
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        public string RoleInstance
        {
            get { return string.IsNullOrEmpty(this.roleInstance) ? null : this.roleInstance; }
            set { this.roleInstance = value; }
        }

        /*internal void UpdateTags(IDictionary<string, string> tags)
        {
            Tags.UpdateTagValue(tags, "ai.cloud.role", this.RoleName);
            Tags.UpdateTagValue(tags, "ai.cloud.roleInstance", this.RoleInstance);
        }
        
        internal void CopyTo(CloudContext target)
        {
            Tags.CopyTagValue(this.RoleName, ref target.roleName);
            Tags.CopyTagValue(this.RoleInstance, ref target.roleInstance);
        }*/
    }
}
