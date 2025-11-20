namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
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
    }
}
