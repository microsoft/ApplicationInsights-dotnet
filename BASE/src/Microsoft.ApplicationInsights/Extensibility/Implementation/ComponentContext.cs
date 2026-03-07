namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    /// <summary>
    /// Encapsulates information describing an Application Insights component.
    /// </summary>
    /// <remarks>
    /// This class matches the "Application" schema concept. We are intentionally calling it "Component" for consistency
    /// with terminology used by our portal and services and to encourage standardization of terminology within our
    /// organization. Once a consensus is reached, we will change type and property names to match.
    /// </remarks>
    public sealed class ComponentContext
    {
        /// <summary>
        /// Environment variable key used to communicate component version to the exporter.
        /// </summary>
        internal const string ComponentVersionEnvironmentVariable = "APPLICATIONINSIGHTS_COMPONENT_VERSION";

        private string version;

        internal ComponentContext()
        {
        }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string Version
        {
            get
            {
                return string.IsNullOrEmpty(this.version) ? null : this.version;
            }

            set
            {
                this.version = value;
                Environment.SetEnvironmentVariable(ComponentVersionEnvironmentVariable, value);
            }
        }
    }
}
