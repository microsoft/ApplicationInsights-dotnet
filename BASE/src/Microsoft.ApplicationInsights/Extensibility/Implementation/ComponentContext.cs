namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    /// <summary>
    /// Encapsulates information describing an Application Insights component.
    /// </summary>
    /// <remarks>
    /// This class matches the "Application" schema concept. We are intentionally calling it "Component" for consistency
    /// with terminology used by our portal and services and to encourage standardization of terminology within our
    /// organization. Once a consensus is reached, we will change type and property names to match.
    /// </remarks>
    internal sealed class ComponentContext
    {
        private string version;

        internal ComponentContext()
        {
        }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string Version
        {
            get { return string.IsNullOrEmpty(this.version) ? null : this.version; }
            set { this.version = value; }
        }
    }
}
